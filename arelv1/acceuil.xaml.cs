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
                    /*
                     * mettre en subrillance les heures 'on verra plus tard...
                    if (j == (4 * jj))
                    {
                        macase.BorderBrush = new SolidColorBrush(Colors.SeaGreen);
                        macase.BorderThickness = new Thickness(1,0,1,1);
                    }                        
                    else
                    {
                        macase.BorderThickness = new Thickness(1);
                        macase.BorderBrush = new SolidColorBrush(Colors.LightBlue);
                    }
                    */
                    macase.BorderThickness = new Thickness(1);
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
            ajoutCours("nfo", "2016-04-27T08:45:00.000+0000", "2016-04-27T09:45:00.000+0000", "Mathematiques CPI 2", "#00b0f0", 26);
        }

        private void ajoutCours(string prof,string heureDebut,string heureFin,string matière,string couleur,int premierJour)
        {
            DateTime dt = Convert.ToDateTime(heureDebut);
            DateTime dt2 = Convert.ToDateTime(heureFin);

            int col = dt.Day - premierJour;
            int ligneDeb = ((dt.Hour - 10)*4)+1;
            int ligneFin = ((dt2.Hour - 10)*4);

            ligneDeb += (dt.Minute/15);
            ligneFin += (dt2.Minute / 15);

            ecrire(ligneDeb.ToString()+" - "+ ligneFin.ToString());

            TextBlock matBlock = new TextBlock();
            matBlock.Text = matière;
            matBlock.FontSize = 13;
            matBlock.HorizontalAlignment = HorizontalAlignment.Center;

            TextBlock profBlock = new TextBlock();
            profBlock.Text = prof;
            profBlock.FontSize = 13;
            profBlock.HorizontalAlignment = HorizontalAlignment.Center;

            


            for (int i=ligneDeb;i<=ligneFin;i++)
            {
                StackPanel macase = new StackPanel();

                if(i == ligneDeb)
                {
                    macase.Children.Add(matBlock);
                }

                if (i == ligneDeb + 1)
                {
                    macase.Children.Add(profBlock);
                }


                macase.Background = new SolidColorBrush(page.HexToColor(couleur));
                
                grid.Children.Add(macase);
                Grid.SetColumn(macase, col);
                Grid.SetRow(macase, i);
            }

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
