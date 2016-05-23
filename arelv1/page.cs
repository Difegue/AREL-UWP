using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;

namespace arelv1
{
    public class Page
    {
        
        private static ManualResetEvent allDone = new ManualResetEvent(false);//pour les events asynchrone
        private string resultat;

        //-------------------- Retourne la date du lundi precedent ---------------------------------------


        //-------------------- convertisseur n° semaine en date --------------------------------------

        public DateTime weekToDate(int week,int year, string day)
        {
            //Jour int ISO
            Dictionary<string, int> dicDays = new Dictionary<string, int>()
            {
                {"lundi", 1 },
                {"mardi", 2 },
                {"mercredi", 3},
                {"jeudi", 4 },
                {"vendredi", 5 },
                {"samedi", 6 },
                {"dimanche", 7 }
            };

            
            

            DateTime value = new DateTime(year, 1, 1).AddDays(7 * week);

            int daysToAdd = dicDays[day.ToLower()];

            // On contrôle si l'année commence après jeudi si oui on décale d'une semaine.
            if ((int)new DateTime(value.Year, 1, 1).DayOfWeek < 5)
            {
                daysToAdd -= (int)value.DayOfWeek + 7;
            }
            else
            {
                daysToAdd -= (int)value.DayOfWeek;
            }

            return value.AddDays(daysToAdd);
        }



        //-------------------- convertisseur hexa -> rgb ---------------------------------------------

        public Color HexToColor(String hexString)
        {
            Color actColor;
            int r, g, b;
            r = 0;
            g = 0;
            b = 0;
            if ((hexString.StartsWith("#")) && (hexString.Length == 7))
            {
                r = HexToInt(hexString.Substring(1, 1))*16 + HexToInt(hexString.Substring(1, 1));
                g = HexToInt(hexString.Substring(3, 1)) * 16 + HexToInt(hexString.Substring(4, 1));
                b = HexToInt(hexString.Substring(5, 1)) * 16 + HexToInt(hexString.Substring(6, 1));
                
                actColor = Color.FromArgb(150, Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
            }
            else
            {
                actColor = Color.FromArgb(0,0,0,0);
            }
            return actColor;
        }

        public int HexToInt(string s)
        {
            int res;
            switch(s)
            {
                case "1":
                    res = 1;
                break;
                case "2":
                    res = 2;
                break;
                case "3":
                    res = 3;
                    break;
                case "4":
                    res = 4;
                    break;
                case "5":
                    res = 5;
                    break;
                case "6":
                    res = 6;
                    break;
                case "7":
                    res = 7;
                    break;
                case "8":
                    res = 8;
                    break;
                case "9":
                    res = 9;
                    break;
                case "a":
                    res = 10;
                    break;
                case "b":
                    res = 11;
                    break;
                case "c":
                    res = 12;
                    break;
                case "d":
                    res = 13;
                    break;
                case "e":
                    res = 14;
                    break;
                case "f":
                    res = 15;
                    break;
                case "0":
                    res = 0;
                break;
                default:
                    res = 0;
                break;
                
            }
            return res;
        }



        //--------------------enregistrer dans un fichier... -----------------------------------------


        private bool readsuccess;


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

        async void getDataAsync(string key)//lecture asynchrone
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(key);
                resultat = await FileIO.ReadTextAsync(file);
                readsuccess = true;
            }
            catch (Exception)
            {
                readsuccess = false;
            }
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
                    resultat = "identifiants incorrects";
                }
                else if (erreur.IndexOf("401") > -1)
                {
                    resultat = "acces refusé";
                }
                else if (erreur.IndexOf("403") > -1)
                {
                    Stream streamResponse = e.Response.GetResponseStream();
                    StreamReader streamRead = new StreamReader(streamResponse);
                    string responseString = streamRead.ReadToEnd();


                    resultat = "ressource interdite: "+responseString;
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
                    resultat = "erreur inconnu...\n" + e.Status;
                }
               

            }
            allDone.Set();
        }
    }
}
