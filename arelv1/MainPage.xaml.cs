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
        private ArelAPI.Connector API = new ArelAPI.Connector();//objet pour faire la requete html et peut un jour d'autres choses
        private bool stayConnect;
        

        //fonction executée quand on appuie sur le bouton
        private void login_button(object sender, RoutedEventArgs ev)
        {

            if(API.connect(nom.Text.ToLower(), pass.Password))
            {
                //C'est vraiment une idée de merde de stocker les credentials en dur comme ça,
                //Tout casse si l'user change son password. 
                //Après sur la doc y'a rien sur comment renouveler les jetons d'authentification proprement du coup on laisse comme ça 
                localSettings.Values["user"] = nom.Text.ToLower();
                localSettings.Values["pass"] = pass.Password;

                if (stayConnect)
                    localSettings.Values["stayConnect"] = true;
                
                Frame.Navigate(typeof(acceuil));
            }
            else
            {
                //L'objet API logge l'erreur de connexion si il en arrive une.
                ecrire("Erreur de connexion: "+API.getData("erreurLogin"));
            }  
        }

        //fonction main
        public MainPage()
        {
            InitializeComponent(); //demarage de l'interface   
            localSettings.Values["internet"] = null;
            stayConnect = false;
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

        private void anonLogin_button(object sender, RoutedEventArgs e)
        {
            if (localSettings.Values["user"] != null && localSettings.Values["pass"] != null && API.isset("planning") && API.isset("salles") && API.isset("notes"))
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
