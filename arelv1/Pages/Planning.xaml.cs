using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace arelv1.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Planning : Page
    {
        private DateTime now = DateTime.Now; //Temps enregistré pour l'affichage du planning
        private ArelApi API = new ArelApi();

        public Planning()
        {
            this.InitializeComponent();

            if (API.isOnline())
                updatePlanning(0);

            DrawPlanning();
            writePlanning(API.getData("planning"));

            UpdateLayout();

        }

        private void calcTaille(object sender, SizeChangedEventArgs e)
        {
            double width = button_week.ActualWidth;

            if (width < 450)
            {
                width = width - 50;
                c1.Width = new GridLength(width);
                c2.Width = new GridLength(width);
                c3.Width = new GridLength(width);
                c4.Width = new GridLength(width);
                c5.Width = new GridLength(width);
            }
            else
            {
                width = (width - 50) / 5;
                c1.Width = new GridLength(width);
                c2.Width = new GridLength(width);
                c3.Width = new GridLength(width);
                c4.Width = new GridLength(width);
                c5.Width = new GridLength(width);
            }
        }

        private DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
            {
                diff += 7;
            }
            return dt.AddDays(-1 * diff).Date;
        }

        private void updatePlanning(int daysExtra)
        {
            now = now.AddDays(daysExtra);
            now = StartOfWeek(now, DayOfWeek.Monday);

            int an = now.Year;
            int mois = now.Month;
            int jour = now.Day;   

            API.saveData("planning", API.getInfo("api/planning/slots?start=" + an + "-" + mois + "-" + jour));
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            //Get new info
            updatePlanning(-7);
            grid.Children.Clear();
            DrawPlanning();
            writePlanning(API.getData("planning"));
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            updatePlanning(7);
            grid.Children.Clear();
            DrawPlanning();
            writePlanning(API.getData("planning"));
        }

        private void entete(string name, int col, Grid grid)
        {
            TextBlock texte = new TextBlock();
            texte.HorizontalAlignment = HorizontalAlignment.Center;
            texte.Text = name;
            grid.Children.Add(texte);
            Grid.SetColumn(texte, col);
            Grid.SetRow(texte, 0);

        }



        private void ajoutCours(string prof, string heureDebut, string heureFin, string matière, string couleur, DateTime premierJour, string salle)
        {
            DateTime now = DateTime.Now;

            DateTime dt = Convert.ToDateTime(heureDebut);
            DateTime dt2 = Convert.ToDateTime(heureFin);

            int col = dt.Day - premierJour.Day + 1;
            if (col < 0 && now.Month == premierJour.Month)
                col = col + DateTime.DaysInMonth(dt.Year, dt.Month - 1);
            int ligneDeb = ((dt.Hour - 8) * 4) + 1;
            int ligneFin = ((dt2.Hour - 8) * 4);
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

                    if (i == ligneDeb + 1)
                    {
                        macase.Children.Add(profBlock);
                    }

                    if (i == ligneDeb + 2)
                    {
                        macase.Children.Add(salleBlock);
                    }




                    macase.Background = new SolidColorBrush(API.HexToColor(couleur));

                    grid.Children.Add(macase);
                    Grid.SetColumn(macase, col);
                    Grid.SetRow(macase, i);
                }
            }

        }

        private void writePlanning(string xml)
        {

            string prof = "";
            string salle = "";
            string matiere = "";
            string debut = "";
            string fin = "";
            string couleur = "";
            string week = "";
            string week1 = "";
            DateTime lundi;
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable

            foreach (System.Xml.XmlNode node in doc.DocumentElement.Attributes)
            {
                if (node.Name == "week")
                {
                    week = node.InnerText;
                    week1 = week.Substring(0, 2);
                }
            }

            numeroSemaine.Text = "Emploi du Temps - Semaine " + week1;
            lundi = API.weekToDate(Convert.ToInt32(week1), 2016, "lundi");


            foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)//on parcours tout les noeuds
            {
                foreach (System.Xml.XmlNode node02 in node)
                {
                    //ecrire(node02.Name);
                    if (node02.Name == "teacherLogin")
                        prof = node02.InnerText;
                    if (node02.Name == "label")
                        matiere = node02.InnerText;
                    if (node02.Name == "departmentColor")
                        couleur = node02.InnerText;
                    if (node02.Name == "beginDate")
                        debut = node02.InnerText;
                    if (node02.Name == "endDate")
                        fin = node02.InnerText;
                    if (node02.Name == "roomLabel")
                        salle = node02.InnerText;
                }

                if (prof != "" && matiere != "" && debut != "" && fin != "" && couleur != "" && salle != "")
                    ajoutCours(prof, debut, fin, matiere, couleur, lundi, salle);

            }



        }

        //Crée un joli string avec la date + un offset de jour
        private string getDayStr(DateTime dt, int daysToAdd)
        {

            dt = dt.AddDays(daysToAdd);

            string s1 = dt.ToString("dddd d ");
            string s2 = dt.ToString("MMMM");

            //On capitalise la première lettre du jour et du mois - ça serait plus clean en faisant un substring jusqu'au second espace et en capitalisant là au lieu de split le string en deux
            //Mais flemme
            char[] a1 = s1.ToCharArray();
            a1[0] = char.ToUpper(a1[0]);
            char[] a2 = s2.ToCharArray();
            a2[0] = char.ToUpper(a2[0]);

            return new string(a1)+new string(a2);
        }


        private void DrawPlanning()
        {

            entete(getDayStr(now, 0), 1, grid);
            entete(getDayStr(now, 1), 2, grid);
            entete(getDayStr(now, 2), 3, grid);
            entete(getDayStr(now, 3), 4, grid);
            entete(getDayStr(now, 4), 5, grid);


            for (int j = 1; j < 41; j = j + 1)
            {
                TextBlock heure = new TextBlock();
                heure.FontSize = 13;
                int jj = (j / 4);
                if (j == (4 * jj))
                    heure.Text = (8 + jj).ToString() + "h ";
                grid.Children.Add(heure);

                Grid.SetColumn(heure, 0);
                Grid.SetRow(heure, j);

                for (int i = 1; i < 6; i++)
                {
                    StackPanel macase = new StackPanel();

                    macase.BorderThickness = new Thickness(2, 0, 0, 0);
                    macase.BorderBrush = new SolidColorBrush(Colors.Black);
                    
                    grid.Children.Add(macase);
                    Grid.SetColumn(macase, i);
                    Grid.SetRow(macase, j);
                }
            }

        }
    }
}
