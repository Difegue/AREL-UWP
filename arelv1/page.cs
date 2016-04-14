using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace arelv1
{
    public class Page
    {
        private static ManualResetEvent allDone = new ManualResetEvent(false);//pour les events asynchrone
        private string resultat;//resultat de la requete
        private string postData;

        public string http(string url,string contentType,string identifiants,string modeAuth,string data)
        {
            WebRequest connexion = WebRequest.Create(url);//url
            allDone = new ManualResetEvent(false);//reinitialisation de l'event asynchrone

            connexion.Method = "POST";//Post ou get
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

            connexion.BeginGetRequestStream(new AsyncCallback(requete), connexion);//lance la connexion asynchrone
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
            catch (Exception e)
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
                    resultat = "ressource interdite";
                }
                else if (erreur.IndexOf("être résolu") > -1)
                {
                    resultat = "Pas de connection internet";
                }
                else
                {
                    resultat = "erreur inconnu...";
                }
               

            }
            allDone.Set();
        }
    }
}
