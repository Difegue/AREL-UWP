using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

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
        }


        private string getinfo(string url)
        {
            url = "https://arel.eisti.fr/api/campus/rooms";
            string contentType = "application/xml";
            string identifiants = localSettings.Values["token"].ToString();
            string data = "format=xml";

            string resultat = page.http(url, contentType, identifiants,"Bearer", data);//on fait la requete
            return resultat;
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
            rootPivot.SelectedIndex = 0;
            bouton_agenda.Foreground = new SolidColorBrush(Colors.Black);
            bouton_note.Foreground = new SolidColorBrush(Colors.White);
            bouton_salles.Foreground = new SolidColorBrush(Colors.White);
        }

        private void noteClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            rootPivot.SelectedIndex = 1;
            bouton_agenda.Foreground = new SolidColorBrush(Colors.White);
            bouton_note.Foreground = new SolidColorBrush(Colors.Black);
            bouton_salles.Foreground = new SolidColorBrush(Colors.White);
        }

        private void sallesClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            rootPivot.SelectedIndex = 2;
            bouton_agenda.Foreground = new SolidColorBrush(Colors.White);
            bouton_note.Foreground = new SolidColorBrush(Colors.White);
            bouton_salles.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void decoClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            localSettings.Values["user"] = null;
            localSettings.Values["pass"] = null;
            Frame.Navigate(typeof(MainPage));
        }

       

        private void pivotClick(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            switch (rootPivot.SelectedIndex)
            {
                case 0:
                    agendaClick(sender, e);
                    break;
                case 1:
                    noteClick(sender, e);
                    break;
                case 2:
                    sallesClick(sender, e);
                    break;


            }
        }
    }
}
