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
        private Page page = new Page();

        public acceuil()
        {
            InitializeComponent();

            if(localSettings.Values["internet"] == null)
            {
                DateTime now = DateTime.Now;
                int an = now.Year;
                int mois = now.Month;
                int jour = now.Day;
                DayOfWeek joursemaine = now.DayOfWeek;


                Dictionary<string, int> dicDays = new Dictionary<string, int>()
                {
                    {"monday", 0 },
                    {"tuesday", 1 },
                    {"wednesday", 2},
                    {"thursday", 3 },
                    {"friday", 4 },
                    {"saturday", 5 },
                    {"sunday", 6 }
                 };
                
                int daysToAdd = dicDays[joursemaine.ToString().ToLower()];

                jour = jour - daysToAdd;
                if(jour < 0)
                {
                    jour = jour + DateTime.DaysInMonth(an, mois - 1);
                    mois--;

                }



                page.saveData("planning", getinfo("api/planning/slots?start="+an+"-"+mois+ "-"+jour));
                page.saveData("salles", getinfo("api/campus/rooms?siteId=1990"));

                page.saveData("user",getinfo("api/me"));
                page.saveData("note", getinfo("api/marks/"+getIdUser(page.getData("user"))+"/export?id="+ getIdUser(page.getData("user"))));
                //ecrire(getinfo("api/campus/romms", "&id=1990"));


            }
            
            
            DrawPlanning();
            writePlanning(page.getData("planning"),1);
            writeSalle(page.getData("salles"));
            //string notes = page.getData("note");

            //Récup du nom de l'utilisateur pour affichage
            string userName = getUserFullName(page.getData("user"));
            initialeUser.Text = userName.Substring(0,1);
            nomUser.Text = userName;

            AgendaBouton.IsChecked = true; //Petit trick pour montrer qu'on est sur l'EDT de base
            UpdateLayout();
        }

        private void writeSalle(string xml)
        {
            //List<StackPanel> salles = new List<StackPanel>();
            //List<String> salles = new List<String>();
            string aff;
            string libre;
            int i = 0;
            TextBlock nomBlock;
            TextBlock libreBlock;
            StackPanel macase;
            SolidColorBrush color;

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable

            //salles.Add("nom de salle".PadRight(15, '.') + "libre?".PadLeft(15,'.'));
            foreach (System.Xml.XmlNode room in doc.GetElementsByTagName("room"))
            {
                aff = "";
                libre = "";
                color = new SolidColorBrush(Colors.Red);
                foreach (System.Xml.XmlNode label in room)
                {
                    if(label.Name == "label")
                    {
                        aff = label.InnerText + ":";
                    }
                    if (label.Name == "bookable")
                    {
                        if(label.InnerText == "true")
                        {
                            libre = " ok";
                            color = new SolidColorBrush(Colors.Green);
                        }
                        else
                        {
                            libre = " non";
                            color = new SolidColorBrush(Colors.Red);
                        }
                        
                    }
                }
                RowDefinition Row = new RowDefinition();
                Row.Height = new GridLength(35);
                salles.RowDefinitions.Add(Row);
                
                nomBlock = new TextBlock();
                libreBlock = new TextBlock();
                macase = new StackPanel();

                nomBlock.Text = aff;
                nomBlock.Foreground = new SolidColorBrush(Colors.SaddleBrown);
                nomBlock.HorizontalAlignment = HorizontalAlignment.Center;

                libreBlock.Text = libre;
                libreBlock.Foreground = color;
                libreBlock.HorizontalAlignment = HorizontalAlignment.Center;

                salles.Children.Add(nomBlock);
                Grid.SetColumn(nomBlock, 0);
                Grid.SetRow(nomBlock, i);

                salles.Children.Add(libreBlock);
                Grid.SetColumn(libreBlock, 1);
                Grid.SetRow(libreBlock, i);


                i++;

                //macase.Children.Add(nomBlock);
                //macase.Children.Add(libreBlock);
                //macase.HorizontalAlignment = HorizontalAlignment.Center;

                //salles.Add(macase);

                //salles.Add(aff.PadRight(30 - aff.Length, '.') + libre);
                //salles.Add (String.Format("{0, 5} {1, 10}", aff, libre));*/
            }

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

        private void writePlanning(string xml,int semaine)
        {
           
            string prof = "";
            string salle = "";
            string matiere = "";
            string debut = "";
            string fin = "";
            string couleur ="";
            string week = "";
            string week1 = "";
            string week2 = "";
            DateTime lundi;
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable

            foreach (System.Xml.XmlNode node in doc.DocumentElement.Attributes)
            {
                if(node.Name == "week")
                {
                    week = node.InnerText;
                    week1 = week.Substring(0, 2);
                    week2 = week.Substring(5, 2);
                }
            }

            foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)//on parcours tout les noeuds
            {
                foreach (System.Xml.XmlNode node02 in node)
                {
                    //ecrire(node02.Name);
                    if(node02.Name == "teacherLogin")
                    {
                        //ecrire(node02.Name);
                        prof = node02.InnerText;
                    }
                       
                    if (node02.Name == "label")
                        matiere = node02.InnerText;
                    if (node02.Name == "departmentColor")
                        couleur = node02.InnerText;
                    if (node02.Name == "beginDate")
                        debut = node02.InnerText;
                    if (node02.Name == "endDate")
                        fin = node02.InnerText;
                    if(node02.Name == "roomLabel")
                        salle = node02.InnerText;
                }
                if (semaine == 1)
                {
                    numeroSemaine.Text = "semaine " + week1;
                    lundi = page.weekToDate(Convert.ToInt32(week1), 2016, "lundi");
                }
                else
                {
                    numeroSemaine.Text = "semaine " + week2;
                    lundi = page.weekToDate(Convert.ToInt32(week2), 2016, "lundi");
                }
                if (prof != "" && matiere != "" && debut != "" &&  fin != "" && couleur != "" && salle != "")
                    ajoutCours(prof, debut, fin, matiere, couleur, lundi,salle);
                
            }



        }



        private void DrawPlanning()
        {

            entete("Lundi", 1, grid);
            entete("Mardi", 2, grid);
            entete("Mercredi", 3, grid);
            entete("Jeudi", 4, grid);
            entete("Vendredi", 5, grid);


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
                    
                    macase.BorderThickness = new Thickness(2,0,0,0);
                    macase.BorderBrush = new SolidColorBrush(Colors.Black);

                    grid.Children.Add(macase);
                    Grid.SetColumn(macase, i);
                    Grid.SetRow(macase, j);
                }
            }

            
         }

        private void entete(string name,int col,Grid grid)
        {
            TextBlock texte = new TextBlock();
            texte.HorizontalAlignment = HorizontalAlignment.Center;
            texte.Text = name;
            grid.Children.Add(texte);
            Grid.SetColumn(texte, col);
            Grid.SetRow(texte, 0);
        }

        private void ajoutCours(string prof,string heureDebut,string heureFin,string matière,string couleur,DateTime premierJour,string salle)
        {
            DateTime now = DateTime.Now;
            
            DateTime dt = Convert.ToDateTime(heureDebut);
            DateTime dt2 = Convert.ToDateTime(heureFin);

            int col = dt.Day - premierJour.Day + 1;
            if (col < 0 && now.Month == premierJour.Month)
                col = col + DateTime.DaysInMonth(dt.Year, dt.Month-1);
            int ligneDeb = ((dt.Hour - 8)*4)+1;
            int ligneFin = ((dt2.Hour - 8)*4);
            if (ligneDeb > 0 && col < 6)
            {
                ligneDeb += (dt.Minute / 15);
                ligneFin += (dt2.Minute / 15);

                //ecrire(ligneDeb.ToString()+" - "+ ligneFin.ToString());

                TextBlock matBlock = new TextBlock();
                matBlock.Text = matière;
                matBlock.FontSize = 13;
                matBlock.HorizontalAlignment = HorizontalAlignment.Center;

                TextBlock profBlock = new TextBlock();
                profBlock.Text = prof;
                profBlock.FontSize = 13;
                profBlock.HorizontalAlignment = HorizontalAlignment.Center;

                TextBlock salleBlock = new TextBlock();
                salleBlock.Text = salle;
                salleBlock.FontSize = 13;
                salleBlock.HorizontalAlignment = HorizontalAlignment.Center;


                for (int i = ligneDeb; i <= ligneFin; i++)
                {
                    StackPanel macase = new StackPanel();

                    if (i == ligneDeb)
                    {
                        macase.Children.Add(matBlock);
                    }

                    if (i == ligneDeb+1)
                    {
                        macase.Children.Add(profBlock);
                    }

                    if (i == ligneDeb + 2)
                    {
                        macase.Children.Add(salleBlock);
                    }




                    macase.Background = new SolidColorBrush(page.HexToColor(couleur));

                    grid.Children.Add(macase);
                    Grid.SetColumn(macase, col);
                    Grid.SetRow(macase, i);
                }
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
            Planning.Visibility = Visibility.Visible;
            Salles.Visibility = Visibility.Collapsed;
            Notes.Visibility = Visibility.Collapsed;
        }

        private void noteClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Planning.Visibility = Visibility.Collapsed;
            Salles.Visibility = Visibility.Collapsed;
            Notes.Visibility = Visibility.Visible;
        }

        private void sallesClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Planning.Visibility = Visibility.Collapsed;
            Salles.Visibility = Visibility.Visible;
            Notes.Visibility = Visibility.Collapsed;
        }

        private void decoClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            localSettings.Values["user"] = null;
            localSettings.Values["pass"] = null;
            localSettings.Values["stayConnect"] = null;
            Frame.Navigate(typeof(MainPage));
        }

        private void calcTaille(object sender, SizeChangedEventArgs e)
        {
            double width = button_week.ActualWidth;
           
            if(width < 450)
            {
                width = width-50;
                c1.Width = new GridLength(width);
                c2.Width = new GridLength(width);
                c3.Width = new GridLength(width);
                c4.Width = new GridLength(width);
                c5.Width = new GridLength(width);
            }
            else
            {
                width = (width -50)/ 5;
                c1.Width = new GridLength(width);
                c2.Width = new GridLength(width);
                c3.Width = new GridLength(width);
                c4.Width = new GridLength(width);
                c5.Width = new GridLength(width);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            
            grid.Children.Clear();
            DrawPlanning();
            writePlanning(page.getData("planning"), 1);
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {

            grid.Children.Clear();
            DrawPlanning();
            writePlanning(page.getData("planning"), 2);
        }
    }
}
