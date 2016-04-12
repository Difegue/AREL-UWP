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
    public class Contexte : INotifyPropertyChanged
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




    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private Contexte contexte;
        private string password,username;
        private string resultat;


        private void login_button(object sender, RoutedEventArgs ev)
        {
            if(connect_login(nom.Text, pass.Password))
            {
                this.Frame.Navigate(typeof(acceuil));
            }            
        }

        private bool connect_login(string name,string pass)
        {
            WebRequest connexion = WebRequest.Create("http://arel.eisti.fr/oauth/token");//url

            connexion.Method = "POST";//Post ou get
            connexion.ContentType = "application/x-www-form-urlencoded";

            string autorization = "win10-19:LTNsH0D0euweCehmWcn9";//identifiants
            byte[] binaryAuthorization = System.Text.Encoding.UTF8.GetBytes(autorization);//conversion en bytes
            autorization = Convert.ToBase64String(binaryAuthorization);
            autorization = "Basic " + autorization;
            connexion.Headers["AUTHORIZATION"] = autorization;//ajout d'une ligne dans le header

            password = pass;
            username = name;

            connexion.BeginGetRequestStream(new AsyncCallback(requete), connexion);//lance la connexion asynchrone
            allDone.WaitOne();//attend et reste sur la page en cours

            if(resultat.IndexOf("tok") > -1)
            {
                return true;
            }
            else
            {
                ecrire(resultat);
                return false;
            }
            

        }



        public MainPage()
        {
            
            this.InitializeComponent();//demarage de l'interface         
            contexte = new Contexte { Valeur = "" };
            DataContext = contexte;//affichage du message
        }

        private void requete(IAsyncResult asynchronousResult)//envoi la requete
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

        private void reponse(IAsyncResult asynchronousResult)//recupère le resultat
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
                else
                {
                    resultat = "erreur inconnu" ;
                }
                //contexte = new Contexte { Valeur = "error: " + erreur };

            }
            allDone.Set();
        }


       

        private void ecrire(string msg)
        {
           contexte.Valeur = msg;
        }
    }
}
