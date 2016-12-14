using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace arelv1
{
   

    public sealed partial class acceuil
    {
        
        private Info contexte; //pour le message d'erreur
        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings; //recuperation d'un tableau pour stocker nos données
        private ArelAPI.Connector API = new ArelAPI.Connector();



        public acceuil()
        {
            this.InitializeComponent();

            InitializeUserInfo();

            //Setting thème
            if (ArelAPI.DataStorage.getData("themePref") == "Dark")
                hamburger.RequestedTheme = ElementTheme.Dark;
            else
                hamburger.RequestedTheme = ElementTheme.Light;

            //Récup du nom de l'utilisateur pour affichage
            string userName = API.GetUserFullName(ArelAPI.DataStorage.getData("user"), "Utilisateur d'AREL");
            nomUser.Text = userName;

            AgendaBouton.IsChecked = true; //Petit trick pour commencer sur l'EDT de base

            
            UpdateLayout();
        }

        private async void InitializeUserInfo()
        {
            Boolean isOnline = await API.IsOnlineAsync();
            if(isOnline)
            {
                //màj des données de l'utilisateur
                string infoUser = await API.GetInfoAsync("/api/me");
                ArelAPI.DataStorage.saveData("user", infoUser);

            }
        }

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

        private void agendaClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            hamburger.IsPaneOpen = false;
            hamburgerContent.Navigate(typeof(Pages.Planning));
            UpdateLayout();
        }

        private void noteClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            hamburger.IsPaneOpen = false;
            hamburgerContent.Navigate(typeof(Pages.Notes));
            UpdateLayout();
        }

        private void sallesClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            hamburger.IsPaneOpen = false;
            hamburgerContent.Navigate(typeof(Pages.Salles));
            UpdateLayout();
        }

        private void absenceClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            hamburger.IsPaneOpen = false;
            hamburgerContent.Navigate(typeof(Pages.Absences));
            UpdateLayout();
        }

        private void settingsClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            hamburger.IsPaneOpen = false;
            hamburgerContent.Navigate(typeof(Pages.Settings));
            UpdateLayout();
        }

        private async void decoClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var dialog = new Windows.UI.Popups.MessageDialog(
                "Voulez-vous vraiment vous déconnecter ? ",
                "Déconnexion");

            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Oui") { Id = 0 });
            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Non") { Id = 1 });

            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;

            var result = await dialog.ShowAsync();

            if ((Int32)result.Id == 0)
            { 
                localSettings.Values["token"] = null;
                localSettings.Values["refresh"] = null;
                localSettings.Values["stayConnect"] = null;

                var vault = new Windows.Security.Credentials.PasswordVault();
                vault.Remove(vault.Retrieve("ARELUWP_User", API.GetUserLogin(ArelAPI.DataStorage.getData("user"))));

                ArelAPI.DataStorage.clearData();
                Frame.Navigate(typeof(MainPage));
            }
        }

    }
}
