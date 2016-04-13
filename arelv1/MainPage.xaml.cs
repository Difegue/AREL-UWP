using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace arelv1
{
    public class Contexte : INotifyPropertyChanged // classe pour gerer le remplissage et la modif du message d'erreur
    {

        private string valeur;

        public string Valeur

        {

            get
            {
                return valeur;
            }

            set
            {
                if (value == valeur)
                    return;
                valeur = value;
                NotifyPropertyChanged("Valeur");
            }

        }


        public event PropertyChangedEventHandler PropertyChanged;


        public void NotifyPropertyChanged(string nomPropriete)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(nomPropriete));
        }

    }




     
    public sealed partial class MainPage : Page //classe principale
    {
        private static ManualResetEvent allDone = new ManualResetEvent(false);//pour les events asynchrone
        private Contexte contexte;//pour le message d'erreur
        private string password,username;
        private string resultat;//resultat de la requete
        private Windows.Storage.ApplicationDataContainer localSettings =  Windows.Storage.ApplicationData.Current.LocalSettings;//recperation d'un tableau pour stocker nos données


        



        //fonction executé quand on appuie sur le bouton
        private void login_button(object sender, RoutedEventArgs ev)
        {
            if(connect_login(nom.Text, pass.Password))
            {
                this.Frame.Navigate(typeof(acceuil));
            }            
        }

        //fonction qui initialise la connection à arel
        private bool connect_login(string name,string pass)
        {
            WebRequest connexion = WebRequest.Create("http://arel.eisti.fr/oauth/token");//url

            connexion.Method = "POST";//Post ou get
            connexion.ContentType = "application/x-www-form-urlencoded";

            string autorization = "win10-19:LTNsH0D0euweCehmWcn9";//identifiants de l'application
            byte[] binaryAuthorization = System.Text.Encoding.UTF8.GetBytes(autorization);//conversion en tableau de la chaine
            autorization = Convert.ToBase64String(binaryAuthorization);
            autorization = "Basic " + autorization;
            connexion.Headers["AUTHORIZATION"] = autorization;//ajout d'une ligne dans le header pour l'authentification

            //passage en variable global des identifiants de l'utilisateur
            password = pass;
            username = name;

            connexion.BeginGetRequestStream(new AsyncCallback(requete), connexion);//lance la connexion asynchrone
            allDone.WaitOne();//attend et reste sur la page en cours

            if(resultat.IndexOf("tok") > -1)//si on trouve tok (en) dans la sortie c'est que c'est bon
            {
                ecrire(getToken(resultat));
                localSettings.Values["token"] = getToken(resultat);
                return true;
            }
            else
            {
                ecrire(resultat);
                return false;
            }
            

        }


        //fonction main
        public MainPage()
        {
            
            this.InitializeComponent();//demarage de l'interface   
            
            //affiche un message d'erreur vide      
            contexte = new Contexte { Valeur = "" };
            DataContext = contexte;
        }

        //recupère le token a partir du resultat de la requete
        private string getToken(string data)
        {
            string res = "toto";
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(data);//chargement de la variable

            foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)//on parcours tout les noeuds
            {
               
                if (node.Name == "access_token")//on recupere le contenu de access_token
                {
                    res = node.InnerText;
                }
            }

            return res;
        }

        //envoi de la requete de connection avec les bons arguments Post
        private void requete(IAsyncResult asynchronousResult)
        {
            

            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

            // End the operation
            Stream postStream = request.EndGetRequestStream(asynchronousResult);


            //string postData = "grant_type=client_credentials&scope=read&format=xml";
            string postData = "grant_type=password&username="+username+"&password="+password+"&scope=read&format=xml";
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
                if(erreur.IndexOf("400") > -1)
                {
                    resultat = "identifiants incorrects";
                }
                else if (erreur.IndexOf("401") > -1)
                {
                    resultat = "acces refusé";
                }
                else if(erreur.IndexOf("être résolu") > -1)
                {
                    resultat = "Pas de connection internet";
                }
                else
                {
                    resultat = "erreur inconnu...";
                }
                //contexte = new Contexte { Valeur = "error: " + erreur };

            }
            allDone.Set();
        }


       
        //fonction pour afficher les erreurs (pourrai être modifier pour faire un pop-up à la place)
        private void ecrire(string msg)
        {
           contexte.Valeur = msg;
        }
    }
}
