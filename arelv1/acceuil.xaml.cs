﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace arelv1
{
   

    public sealed partial class acceuil
    {
        
        private Info contexte;//pour le message d'erreur
        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;//recperation d'un tableau pour stocker nos données
        private Page page = new Page();

        public acceuil()
        {
            InitializeComponent();
            ecrire(getinfo("api/events"));
            //ecrire(localSettings.Values["token"].ToString());
        }


        private string getinfo(string url)
        {
            url = "https://arel.eisti.fr/api/campus/sites";
            string contentType = "application/xml";
            string identifiants = localSettings.Values["token"].ToString();
            string data = "format=xml";

            string resultat = page.http(url, contentType, identifiants,"Bearer", data);//on fait la requete
            return resultat;
        }
        /*
        private void requete(IAsyncResult asynchronousResult)
        {


            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

            // End the operation
            Stream postStream = request.EndGetRequestStream(asynchronousResult);


            //string postData = "grant_type=client_credentials&scope=read&format=xml";
            string postData = "format=xml";
            // Convert the string into a byte array.
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Write to the request stream.
            postStream.Write(byteArray, 0, postData.Length);

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(reponse), request);
        }

        //recupère le résultat de la requete
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
                else if (erreur.IndexOf("être résolu") > -1)
                {
                    resultat = "Pas de connection internet";
                }
                else if(erreur.IndexOf("403") > -1)
                {
                    resultat = "ressource interdite";
                }
                else
                {
                    resultat = "erreur inconnu..."+e;
                }
                //contexte = new Contexte { Valeur = "error: " + erreur };

            }
            allDone.Set();
        }
        */







        

        private void HamburgerButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            hamburger.IsPaneOpen = !hamburger.IsPaneOpen;
        }

        private void ecrire(string msg)
        {
            if (contexte != null)
                contexte.Valeur = msg;
            else
            {
                contexte = new Info { Valeur = msg };
                DataContext = contexte;
            }
        }
    }
}
