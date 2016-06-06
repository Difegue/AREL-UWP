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
using System.Threading.Tasks;

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

        private void txtPassword_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                login_button(sender, e);
        }

        //fonction executée quand on appuie sur le bouton
        private async void login_button(object sender, RoutedEventArgs ev)
        {
            loginProgress.IsActive = true;
            ecrire("");
            //loginProgress.Visibility = Visibility.Visible;
            await Task.Delay(500);

            //Bypass pour le test de certification Windows Store: 
            //Donne des tokens préfaits pour interaction avec l'API sans utiliser de vrais comptes d'utilisateur
            if (nom.Text.ToLower()=="windows10test" && pass.Password == "Developers") 
            {
                localSettings.Values["token"] = "46c0cd11-ffff-4321-bb94-9cee056049d1";
                localSettings.Values["refresh"] = "c7f7f03d-2385-4d8d-a0bc-12035d267f4f";

                Frame.Navigate(typeof(acceuil));
                return;
            }
            else
            if(API.connect(nom.Text.ToLower(), pass.Password))
            {

                if (stayConnect)
                    localSettings.Values["stayConnect"] = true;
                
                Frame.Navigate(typeof(acceuil));
            }
            else
            {
                //L'objet API logge l'erreur de connexion si il en arrive une.
                ecrire("Erreur de connexion: " + API.getData("erreurLogin"));
                loginProgress.IsActive = false;
                await Task.Delay(500);
            }  
        }

        //fonction main
        public MainPage()
        {
            InitializeComponent(); //demarage de l'interface   
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

    }
}
