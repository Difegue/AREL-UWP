using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.Foundation;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI;

namespace ArelAPI
{
    //Classe générique pour traiter avec l'API d'Arel.
    public sealed class Connector
    {

        private static ManualResetEvent allDone = new ManualResetEvent(false);//pour les events asynchrone
        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();

        //fonction qui initialise la connection à arel et stocke jeton d'accès / jeton de refresh

        public IAsyncOperation<bool> LoginARELUserAsync(string name, string pass)
        {
            Task<bool> load = LoginARELUser(name, pass);
            IAsyncOperation<bool> to = load.AsAsyncOperation();
            return to;
        }

        private async Task<bool> LoginARELUser(string name, string pass)
        {

            var client = new HttpClient();
            client.BaseAddress = new Uri("https://arel.eisti.fr");
            var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/token");

            //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var byteArray = new UTF8Encoding().GetBytes(loader.GetString("APIKey"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var formData = new List<KeyValuePair<string, string>>();
            formData.Add(new KeyValuePair<string, string>("grant_type", "password"));
            formData.Add(new KeyValuePair<string, string>("username", name));
            formData.Add(new KeyValuePair<string, string>("password", pass));
            formData.Add(new KeyValuePair<string, string>("scope", "read"));
            formData.Add(new KeyValuePair<string, string>("format", "xml"));

            request.Content = new FormUrlEncodedContent(formData);

            var response = await client.SendAsync(request);


            string resultat = await response.Content.ReadAsStringAsync();

            if (resultat.IndexOf("tok") > -1)//si on trouve tok (en) dans la sortie c'est que c'est bon
            {
                localSettings.Values["token"] = GetToken(resultat, "access_token");//on save le token

                return true;
            }
            else
            {
                
                string erreur = response.StatusCode.ToString();

                switch(response.StatusCode)
                {
                    case (HttpStatusCode.BadRequest):
                        erreur = "Identifiants incorrects";
                        break;

                    case (HttpStatusCode.Unauthorized):
                        erreur = "Accès refusé";
                        break;

                    case (HttpStatusCode.Forbidden):
                        erreur = "Ressource interdite";
                        break;

                    case (HttpStatusCode.InternalServerError):
                        erreur = "Erreur Serveur";
                        break;

                    case (HttpStatusCode.NotFound):
                        erreur = "Endpoint introuvable.";
                        break;

                    default:
                        break;

                } 

                DataStorage.saveData("erreurLogin", erreur);
                return false;
            }
        }

        //recupère le token a partir du resultat de la requete
        private string GetToken(string data, string type)
        {
            string res = "toto";
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(data);//chargement de la variable

            foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)//on parcours tout les noeuds
                if (node.Name == type)//on recupere le contenu de access_token
                    res = node.InnerText;

            return res;
        }

        //Récupère les données d'un endpoint API AREL spécifié. Renvoie des données de test si le test mode est actif.
        public IAsyncOperation<string> GetInfoAsync (string url)
        {
            Task<string> load = GetInfo(url);
            IAsyncOperation<string> to = load.AsAsyncOperation();
            return to;
        }

        private async Task<string> GetInfo(string url)
        {
            if (DataStorage.isTestModeEnabled())
            {
                return GetTestData(url);
            }
            else
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri("https://arel.eisti.fr");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", localSettings.Values["token"].ToString());

                var formData = new List<KeyValuePair<string, string>>();
                formData.Add(new KeyValuePair<string, string>("format", "xml"));

                request.Content = new FormUrlEncodedContent(formData);

                var response = await client.SendAsync(request);

                string resultat = await response.Content.ReadAsStringAsync();

                return resultat;
            }
        }

        //Renvoie différentes données de test selon l'endpoint spécifié.
        private String GetTestData(String url)
        {
            //Les strings sont obtenus de TestData.resw
            Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader("TestData");

            string returnData = "<NoTestDataAvailable/>";

            if (url.StartsWith("/api/planning/slots?start="))
                returnData = loader.GetString("planningTestXml");

            if (url.StartsWith("/api/planning/slots/"))
                returnData = loader.GetString("absenceSlotTestXml");

            if (url.StartsWith("/api/rels/"))
                returnData = loader.GetString("relTestXml");

            switch (url)
            {
                case "/api/me":
                    returnData = loader.GetString("userTestXml");
                    break;
                case "/api/me/absences":
                    returnData = loader.GetString("absencesTestXml");
                    break;
                case "/api/campus/sites":
                    returnData = loader.GetString("campusTestXml");
                    break;
                case "/api/campus/rooms?siteId=1991": //test data dispo pour cergy seulement parce que flemme
                    returnData = loader.GetString("sallesTestXml");
                    break;
                case "/api/me/marks":
                    returnData = loader.GetString("notesTestXml");
                    break;
            }

            return returnData;
        }


        //Récupère le login stocké dans le Credential Locker et l'utilise pour réobtenir un access token.

        public IAsyncOperation<bool> RenewAccessTokenAsync()
        {
            Task<bool> load = RenewAccessToken();
            IAsyncOperation<bool> to = load.AsAsyncOperation();
            return to;
        }

        private async Task<bool> RenewAccessToken()
        {

            var vault = new Windows.Security.Credentials.PasswordVault();
            string login = GetUserLogin(ArelAPI.DataStorage.getData("user"));

            //si le login est le string de fallback, on a pas d'user enregistré => échec
            if (login == "user")
                return false;

            PasswordCredential passwd = vault.Retrieve("ARELUWP_User", login);

            bool result = await LoginARELUser(login, passwd.Password);

            //Si result = true, un nouvel access token a été stocké.
            return result;
        }

        //Check if our connection to the Arel API is still in good standing (token hasn't expired or website isn't down)
        public IAsyncOperation<bool> IsOnlineAsync()
        {
            Task<bool> load = IsOnline();
            IAsyncOperation<bool> to = load.AsAsyncOperation();
            return to;
        }

        private async Task<bool> IsOnline()
        {

            if (DataStorage.isTestModeEnabled()) //Pas de connexion à l'API nécessaire en test mode
                return true;

            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
                string data = await GetInfo("/api/me");
                doc.LoadXml(data); //On essaie de charger un call bidon, si on obtient un XML correct (et pas "Accès Refusé") on est OK

                //On peut aussi avoir un xml d'erreur spécifiant que le token est invalide, auquel cas on retourne false
                if (data.Contains("invalid_token"))
                    return false;
                else
                    return true;
            }
            catch (System.Xml.XmlException) //Une exception sera lancée si notre jeton est invalide
            {
                return false;
            }

        }

        //Met à jour un calendrier Windows custom avec les données de planning de l'API Arel.
        //On peut par la suite ouvrir l'appli calendrier Windows sur ce cal. custom.
        public async void UpdateWindowsCalendar(string start, string end, string calendarName)
        {
            
            string apiUrl = "api/planning/slots?start=" + start + "&end=" + end;

            string planningXML = await GetInfo(apiUrl);

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            try
            {
                doc.LoadXml(planningXML); //chargement de la variable
            }
            catch (Exception)
            {
                return; //Pas très catholique tout ça
            }
            //On a le XML, on ouvre le calendrier custom

            // 1. get access to appointmentstore 
            var appointmentStore = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);

            // 2. get calendar 

            AppointmentCalendar calendar
                = (await appointmentStore.FindAppointmentCalendarsAsync())
                         .FirstOrDefault(c => c.DisplayName == calendarName);

            if (calendar == null)
                calendar = await appointmentStore.CreateAppointmentCalendarAsync(calendarName);

            //Et c'est parti pour la boucle de la folie
            foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)
            {
                // 3. create new Appointment 
                var appo = new Windows.ApplicationModel.Appointments.Appointment();

                DateTime startDate = DateTime.Parse(node.ChildNodes[0].InnerText);
                DateTime endDate = DateTime.Parse(node.ChildNodes[1].InnerText);

                // appointment properties 
                appo.AllDay = false;
                appo.Location = node.ChildNodes[6].InnerText;
                appo.StartTime = startDate;
                appo.Duration = new TimeSpan(0, (int)(endDate - startDate).TotalMinutes, 0);


                //Récup non complet rel (aka matière/sujet)
                string idRel = node.ChildNodes[2].InnerText;
                string xmlr = await GetInfo("/api/rels/" + idRel);
                string relName = getRelName(xmlr, node.ChildNodes[11].InnerText);

                //Récup nom complet prof
                string idProf = node.ChildNodes[3].InnerText;
                string xmlj = await GetInfo("/api/users/" + idProf);
                string profName = GetUserFullName(xmlj, node.ChildNodes[4].InnerText);
                appo.Organizer = new Windows.ApplicationModel.Appointments.AppointmentOrganizer();
                appo.Organizer.DisplayName = profName;

                appo.Subject = relName + " - " + profName;

                //Est-ce que cet appointment exact existe déjà 
                //On regarde les appointments sur ce créneau

                Appointment apCheck = (await calendar.FindAppointmentsAsync(appo.StartTime, appo.Duration)).FirstOrDefault(a => a.Subject == appo.Subject);
                //Si il en existe un sur ce créneau, on l'efface avant d'ajouter le nouveau

                if (apCheck != null)
                    await calendar.DeleteAppointmentAsync(apCheck.LocalId);

                await calendar.SaveAppointmentAsync(appo);

            }

        }


        //Fonctions de traitement des XML renvoyés par l'API

        public string getRelName(string xmlr, string fallback)
        {
            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
                doc.LoadXml(xmlr);//chargement de la variable
                return doc.ChildNodes[0].ChildNodes[0].InnerText;
            }
            catch (Exception) //On renvoie le string de fallback si le parsing échoue
            { return fallback; }
        }


        //Récupère l'ID de l'utlisateur avec le XML de getUserInfo.
        public string GetIdUser(string xml)
        {
            string userid = "0";
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable
            foreach (System.Xml.XmlNode node in doc.DocumentElement.Attributes)
            {
                if (node.Name == "id")
                {
                    userid = node.InnerText;
                }
            }
            return userid;
        }

        //Récupère le nom complet de l'utilisateur avec le XML de getUserInfo.
        public string GetUserFullName(string xml, string fallback)
        {
            string fn = "User";
            string ln = "Anonyme";
            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
                doc.LoadXml(xml);//chargement de la variable
                foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.Name.ToLower() == "firstname")
                        fn = node.InnerText;

                    if (node.Name.ToLower() == "lastname")
                        ln = node.InnerText;
                }
                return fn + " " + ln;
            }
            catch (Exception) //On renvoie le string de fallback si le parsing échoue
            { return fallback; }
        }

        public string GetUserLogin(string xml)
        {
            string ret = "user";
            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
                doc.LoadXml(xml);//chargement de la variable
                foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.Name.ToLower() == "username")
                        ret = node.InnerText;
                }
            }
            catch (Exception)
            { }

            return ret;
        }
    }
}
