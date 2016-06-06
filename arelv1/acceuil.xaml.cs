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

            if(API.isOnline())
            {
                //màj des données de l'utilisateur
                API.saveData("user", API.getInfo("api/me"));
            }

            //Récup du nom de l'utilisateur pour affichage
            string userName = API.getUserFullName(API.getData("user"));
            nomUser.Text = userName;

            AgendaBouton.IsChecked = true; //Petit trick pour commencer sur l'EDT de base
            UpdateLayout();
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

        private void aboutClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            hamburger.IsPaneOpen = false;
            hamburgerContent.Navigate(typeof(Pages.About));
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
                API.clearData();
                Frame.Navigate(typeof(MainPage));
            }
        }

        
    }
}
