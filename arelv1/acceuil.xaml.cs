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
        private ArelApi API = new ArelApi();

        

        public acceuil()
        {
            this.InitializeComponent();

            if(API.isOnline())
            {
                
                API.saveData("user", API.getInfo("api/me"));
                //API.saveData("note", API.getInfo("api/marks/"+getIdUser(API.getData("user"))+"/export?id="+ getIdUser(API.getData("user"))));
                //ecrire(getinfo("api/campus/romms", "&id=1990"));
            }
            
            //string notes = API.getData("note");

            //Récup du nom de l'utilisateur pour affichage
            string userName = getUserFullName(API.getData("user"));
            nomUser.Text = userName;

            AgendaBouton.IsChecked = true; //Petit trick pour commencer sur l'EDT de base
            UpdateLayout();
        }

       

        

        private string getIdUser(string xml)
        {
            string userid = "0";
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable
            foreach (System.Xml.XmlNode node in doc.DocumentElement.Attributes)
            {
                if (node.Name == "id")
                {
                    userid = node.InnerText;
                }
            }
            return userid;
        }

        private string getUserFullName(string xml)
        {
            string ret = "User Anonyme";
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable
            foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.Name == "firstName")
                {
                    ret = node.InnerText;
                }

                if (node.Name == "lastName")
                {
                    ret += " " + node.InnerText;
                }
            }
            return ret;
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

        private void decoClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            localSettings.Values["user"] = null;
            localSettings.Values["pass"] = null;
            localSettings.Values["stayConnect"] = null;
            Frame.Navigate(typeof(MainPage));
        }

        
    }
}
