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
    public class plann : INotifyPropertyChanged
    {
        private List<string> prenoms;

        public List<string> Prenoms
        {

            get { return prenoms; }

            set { NotifyPropertyChanged(ref prenoms, value); }

        }


        public event PropertyChangedEventHandler PropertyChanged;


        public void NotifyPropertyChanged(string nomPropriete)

        {

            if (PropertyChanged != null)

                PropertyChanged(this, new PropertyChangedEventArgs(nomPropriete));

        }


        private bool NotifyPropertyChanged<T>(ref T variable, T valeur, [System.Runtime.CompilerServices.CallerMemberName] string nomPropriete = null)

        {

            if (object.Equals(variable, valeur)) return false;


            variable = valeur;

            NotifyPropertyChanged(nomPropriete);

            return true;

        }
    }

    public class Info : INotifyPropertyChanged // classe pour gerer le remplissage et la modif du message d'erreur
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




     
    public partial class MainPage//classe principale
    {
       
        private Info contexte;//pour le message d'erreur
        private Windows.Storage.ApplicationDataContainer localSettings =  Windows.Storage.ApplicationData.Current.LocalSettings;//recperation d'un tableau pour stocker nos données
        private Page page = new Page();//objet pour faire la requete html et peut un jour d'autres choses
        private bool stayConnect;
        
        


        //fonction executé quand on appuie sur le bouton
        private void login_button(object sender, RoutedEventArgs ev)
        {

            if(connect_login(nom.Text.ToLower(), pass.Password))
            {
                localSettings.Values["user"] = nom.Text.ToLower();
                localSettings.Values["pass"] = pass.Password;
                if (stayConnect)
                {
                    localSettings.Values["stayConnect"] = true;
                    
                }
                
                Frame.Navigate(typeof(acceuil));
            } 
            //ecrire(localSettings.Values["token"].ToString());      
        }

        //fonction qui initialise la connection à arel
        private bool connect_login(string name,string pass)
        {
            string url = "http://arel.eisti.fr/oauth/token";
            string contentType = "application/x-www-form-urlencoded";
            string identifiants = "win10-19:LTNsH0D0euweCehmWcn9";
            string data = "grant_type=password&username="+name+"&password="+pass+"&scope = read&format=xml";

            string resultat = page.http(url, contentType, identifiants,"Basic",data,"POST");//on fait la requete
            

            if (resultat.IndexOf("tok") > -1)//si on trouve tok (en) dans la sortie c'est que c'est bon
            {
                ecrire(getToken(resultat));
                localSettings.Values["token"] = getToken(resultat);//on save le token
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
            
            InitializeComponent();//demarage de l'interface   
            localSettings.Values["internet"] = null;
            stayConnect = false;
             
            
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

       
        //fonction pour afficher les erreurs (pourrai être modifier pour faire un pop-up à la place)
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

        private void stayConnectBox(object sender, RoutedEventArgs e)
        {
            stayConnect = !stayConnect;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void internet_button(object sender, RoutedEventArgs e)
        {
            if (localSettings.Values["user"] != null && localSettings.Values["pass"] != null && page.isset("planning") && page.isset("salles") && page.isset("notes"))
            {
                localSettings.Values["internet"] = true;
                Frame.Navigate(typeof(acceuil));
            }
            else
            {
                ecrire("Une première connexion est requise");
            }
        }
    }
}
