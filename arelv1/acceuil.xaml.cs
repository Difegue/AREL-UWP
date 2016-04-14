using System;
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
    }
}
