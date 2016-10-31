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

            string login = nom.Text.ToLower();
            string pwd = pass.Password;

            if (pwd == "" || login == "")
            {
                ecrire("Veuillez entrer vos identifiants.");
                return;
            }

            loginProgress.IsActive = true;
            ecrire("");
            //loginProgress.Visibility = Visibility.Visible;
            await Task.Delay(500);


            //Bypass pour le test de certification Windows Store: 
            //Active le mode TEST pour cette session, qui fournira pour chaque call API des données préparées à l'avance.
            if (login=="windows10test" && pwd== "Developers") 
            {
                ArelAPI.DataStorage.enableTestMode();
                Frame.Navigate(typeof(acceuil));
                return;
            }
            

            if(API.connect(login,pwd))
            {

                if (stayConnect)
                    localSettings.Values["stayConnect"] = true;
                
                Frame.Navigate(typeof(acceuil));
            }
            else
            {
                //L'objet API logge l'erreur de connexion si il en arrive une.
                ecrire("Erreur de connexion: " + ArelAPI.DataStorage.getData("erreurLogin"));
                loginProgress.IsActive = false;
                await Task.Delay(500);
            }  
        }

        //fonction main
        public MainPage()
        {
            InitializeComponent(); //demarage de l'interface   
            stayConnect = false;

            ArelAPI.DataStorage.disableTestMode();
            ArelAPI.DataStorage.clearData(); //On wipe les données vu qu'on est arrivé sur un écran de connexion

            if (ArelAPI.DataStorage.isset("erreurRefresh") && ArelAPI.DataStorage.getData("erreurRefresh")!="") //Si le refresh token a échoué, on indique pourquoi l'utilisateur n'est plus connecté en lui redemandant son login
            {
               ecrire("Erreur API lors de la reconnexion: " + ArelAPI.DataStorage.getData("erreurRefresh"));
                ArelAPI.DataStorage.saveData("erreurRefresh", "");
            }
               
            if (localSettings.Values.ContainsKey("stayConnect"))
                if ((bool)localSettings.Values["stayConnect"] == true) //Si l'utilisateur a déjà coché Rester Connecté, on le laisse
                    scBox.IsChecked = true;

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
