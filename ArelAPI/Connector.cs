﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.Storage;
using Windows.UI;

namespace ArelAPI
{
    //Classe générique pour traiter avec l'API d'Arel.
    public sealed class Connector
    {
        
        private static ManualResetEvent allDone = new ManualResetEvent(false);//pour les events asynchrone
        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings; 
        private string resultat;

        //fonction qui initialise la connection à arel et stocke un token d'accès
        public bool connect(string name, string pass)
        {
            string url = "http://arel.eisti.fr/oauth/token";
            string contentType = "application/x-www-form-urlencoded";
            string identifiants = "win10-19:LTNsH0D0euweCehmWcn9";
            string data = "grant_type=password&username=" + name + "&password=" + pass + "&scope = read&format=xml";

            string resultat = http(url, contentType, identifiants, "Basic", data, "POST");//on fait la requete

            if (resultat.IndexOf("tok") > -1)//si on trouve tok (en) dans la sortie c'est que c'est bon
            {
                localSettings.Values["token"] = getToken(resultat);//on save le token
                return true;
            }
            else
            {
                
                saveData("erreurLogin", resultat);
                return false;
            }
        }

        //recupère le token a partir du resultat de la requete
        private string getToken(string data)
        {
            string res = "toto";
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(data);//chargement de la variable

            foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)//on parcours tout les noeuds
                if (node.Name == "access_token")//on recupere le contenu de access_token
                    res = node.InnerText;

            return res;
        }

        public string getInfo(string url)
        {
            url = "https://arel.eisti.fr/" + url;
            string contentType = "application/xml";
            string identifiants = localSettings.Values["token"].ToString();
            string data = "format=xml";

            string resultat = http(url, contentType, identifiants, "Bearer", data, "GET");//on fait la requete
            return resultat;
        }

        //Récupère l'ID de l'utlisateur avec le XML de getUserInfo.
        public string getIdUser(string xml)
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
        public string getUserFullName(string xml)
        {
            string fn = "User";
            string ln = "Anonyme";
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



        //Check if our connection to the Arel API is still in good standing (token hasn't expired or website isn't down)
        public bool isOnline()
        {
            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
                doc.LoadXml(getInfo("/api/me")); //On essaie de charger un call bidon
                return true;
            }
            catch (System.Xml.XmlException e)
            {
                return false;
            } 

        }

        //Met à jour un calendrier Windows custom avec les données de planning de l'API Arel.
        //On peut par la suite ouvrir l'appli calendrier Windows sur ce cal. custom.
        public async void updateWindowsCalendar(string start, string end, string calendarName)
        {
            string apiUrl = "api/planning/slots?start=" + start + "&end=" + end;

            string planningXML = getInfo(apiUrl);

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(planningXML);//chargement de la variable
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

                //Récup nom complet prof
                string idProf = node.ChildNodes[3].InnerText;
                string xmlj = getInfo("/api/users/" + idProf);
                string profName = getUserFullName(xmlj);
                appo.Organizer = new Windows.ApplicationModel.Appointments.AppointmentOrganizer();
                appo.Organizer.DisplayName = profName;

                appo.Subject = node.ChildNodes[11].InnerText + profName;

                //Est-ce que cet appointment exact existe déjà 
                //On regarde les appointments sur ce créneau

                Appointment apCheck = (await calendar.FindAppointmentsAsync(appo.StartTime, appo.Duration)).FirstOrDefault(a => a.Subject == appo.Subject);
                //Si il en existe un sur ce créneau, on l'efface avant d'ajouter le nouveau

                if (apCheck != null)
                    await calendar.DeleteAppointmentAsync(apCheck.LocalId);

                await calendar.SaveAppointmentAsync(appo);

            }

        }

        //--------------------enregistrer dans un fichier... -----------------------------------------

        public void saveData(string key, string data)//ecriture normale
        {
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(key, FileMode.Create, IsolatedStorageFile.GetUserStoreForApplication()))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(data);
                }
            }
        }

        public string getData(string key)
        {
            string res;
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(key, FileMode.Open, IsolatedStorageFile.GetUserStoreForApplication()))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    res = reader.ReadLine();
                }
            }
            return res;
        }

        public bool isset(string key)
        {
            IsolatedStorageFile racine = IsolatedStorageFile.GetUserStoreForApplication();
            return racine.FileExists(key);      
        }


        async void saveDataAsync(string key,string data)//ecriture asynchrone
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, data);
        }

        async void getDataAsync(string key) //lecture asynchrone
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(key);
            resultat = await FileIO.ReadTextAsync(file);
        }


        //--------------------------------requete http-------------------------------------

        
        //private string resultat;//resultat de la requete
        private string postData;

        public string http(string url,string contentType,string identifiants,string modeAuth,string data,string toc)
        {
            WebRequest connexion = WebRequest.Create(url);//url
            allDone = new ManualResetEvent(false);//reinitialisation de l'event asynchrone

            connexion.Method = toc;//Post ou get
            //return connexion.Method;
            connexion.Headers["ACCEPT"] = "application/xml";
            connexion.ContentType = contentType;

            string autorization = "";
            postData = data;

            //pour les identifiants
            if(modeAuth == "Bearer")
            {
               autorization = identifiants;
               autorization = modeAuth + " " + autorization;
            }
            else
            {
                autorization = identifiants;//identifiants de l'application
                byte[] binaryAuthorization = System.Text.Encoding.UTF8.GetBytes(autorization);//conversion en tableau de la chaine
                autorization = Convert.ToBase64String(binaryAuthorization);
                autorization = modeAuth + " " + autorization; 
            }
            connexion.Headers["AUTHORIZATION"] = autorization;

            if (connexion.Method == "GET")
            {
                //connexion.BeginGetResponse(this.ResponseCallback, state);
                connexion.BeginGetResponse(new AsyncCallback(reponse), connexion);
            }
            else
            {
                connexion.BeginGetRequestStream(new AsyncCallback(requete), connexion);//lance la connexion asynchrone
            }

            allDone.WaitOne();//attend et reste sur la page en cours
            return resultat;  
            
        }

       

        private void requete(IAsyncResult asynchronousResult)
        {
            
            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

            // End the operation
            Stream postStream = request.EndGetRequestStream(asynchronousResult);
            
            // Convert the string into a byte array.
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Write to the request stream.
            postStream.Write(byteArray, 0, postData.Length);

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(reponse), request);
        }

        private void reponse(IAsyncResult asynchronousResult)
        {


            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

            // End the operation
            try
            {


                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                Stream streamResponse = response.GetResponseStream();
                StreamReader streamRead = new StreamReader(streamResponse);
                string responseString = streamRead.ReadToEnd();
                
                resultat = responseString;
            }
            catch (WebException e)
            {
                
                string erreur = e.ToString();
                if (erreur.IndexOf("400") > -1)
                {                   
                    resultat = "Identifiants incorrects";
                }
                else if (erreur.IndexOf("401") > -1)
                {
                    resultat = "Accès refusé";
                }
                else if (erreur.IndexOf("403") > -1)
                {
                    Stream streamResponse = e.Response.GetResponseStream();
                    StreamReader streamRead = new StreamReader(streamResponse);
                    string responseString = streamRead.ReadToEnd();


                    resultat = "Ressource interdite: "+responseString;
                }
                else if (e.Status.ToString().IndexOf("ResolutionFailure") > -1)
                {
                    resultat = "Pas de connection internet";
                }
                else if(erreur.IndexOf("500") > -1)
                {
                    resultat = "Erreur du serveur";
                }
                else
                {
                    resultat = "Erreur Inconnue.\n" + e.Status;
                }
               

            }
            allDone.Set();
        }
    }
}