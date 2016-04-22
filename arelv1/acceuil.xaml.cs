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
        
        private Info contexte;//pour le message d'erreur
        private plann planning;
        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;//recperation d'un tableau pour stocker nos données
        private Page page = new Page();

        public acceuil()
        {
            InitializeComponent();
            if(localSettings.Values["internet"] == null)
            {
                page.saveData("planning", getinfo("api/planning/slots"));
                page.saveData("salles", getinfo("api/campus/rooms"));
                page.saveData("notes", "toto");
                
            }
               writePlanning(page.getData("planning"));
            UpdateLayout();
            
        }

        private void writePlanning(string xml)
        {
            
            for (int j = 1; j < 41; j=j+1)                
            {
                TextBlock heure = new TextBlock();
                heure.FontSize = 13;
                int jj = (j / 4);
                if(j == (4*jj))
                    heure.Text = (8+jj).ToString() + "h ";
                grid.Children.Add(heure);
                Grid.SetColumn(heure, 0);
                Grid.SetRow(heure, j);

                for (int i = 1; i < 6; i++)
                {
                    StackPanel macase = new StackPanel();
                    macase.BorderThickness = new Thickness(1);
                    if (j == (4 * jj))
                        macase.BorderBrush = new SolidColorBrush(Colors.SeaGreen);
                    else
                        macase.BorderBrush = new SolidColorBrush(Colors.LightBlue);

                    grid.Children.Add(macase);
                    Grid.SetColumn(macase, i);
                    Grid.SetRow(macase, j);
                }
            }
           
            /*
            List<string> l = new List<string> { };
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable

            foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)//on parcours tout les noeuds
            {
                l.Add(node.Name);
            }
            planning = new plann { Prenoms = l};
            DataContext = planning;*/
        }

        private string getinfo(string url)
        {
            url = "https://arel.eisti.fr/"+url;
            string contentType = "application/xml";
            string identifiants = localSettings.Values["token"].ToString();
            string data = "format=xml";

            string resultat = page.http(url, contentType, identifiants,"Bearer", data,"GET");//on fait la requete
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
            localSettings.Values["stayConnect"] = null;
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

        private void calcTaille(object sender, SizeChangedEventArgs e)
        {
            double width = ActualWidth;
            if(width < 510)
            {
                width = width - 120;
                c1.Width = new GridLength(width);
                c2.Width = new GridLength(width);
                c3.Width = new GridLength(width);
                c4.Width = new GridLength(width);
                c5.Width = new GridLength(width);
            }
            else
            {
                width = (width -120)/ 5;
                c1.Width = new GridLength(width);
                c2.Width = new GridLength(width);
                c3.Width = new GridLength(width);
                c4.Width = new GridLength(width);
                c5.Width = new GridLength(width);
            }
        }
    }
}
