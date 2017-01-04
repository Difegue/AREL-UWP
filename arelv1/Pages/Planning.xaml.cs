using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.ApplicationModel.Background;
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

namespace arelv1.Pages
{
    public sealed partial class Planning : Page
    {
        //Temps enregistré pour l'affichage du planning
        private DateTime now = DateTime.Now;
        private ArelAPI.Connector API = new ArelAPI.Connector();
       

        public Planning()
        {
            this.InitializeComponent();

            dateJour.Text = GetDayStr(now, 0);

            FirstGrid.Visibility = Visibility.Collapsed;
            SecondGrid.Visibility = Visibility.Collapsed;

            UpdatePlanningAsync(); //Stocke le planning du jour dans la clé "planning" de l'appli si on a internet

            

        }

        //Crée un joli string avec la date + un offset de jour
        private string GetDayStr(DateTime dt, int daysToAdd)
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

            return new string(a1) + new string(a2);
        }


        /*
         * Les fonctions ci-dessous concernent le dessin de l'EDT sur des élements XAML Grid.
         */

        private async void UpdatePlanningAsync()
        {
            Boolean isOnline = await API.IsOnlineAsync();

            if (isOnline)
            {
                //now = now.AddDays(daysExtra);

                string xmlToday = await API.GetInfoAsync("/api/planning/slots?start=" + now.ToString("yyyy-MM-dd") + "&end=" + now.AddDays(1).ToString("yyyy-MM-dd"));
                string xmlTomorrow = await API.GetInfoAsync("/api/planning/slots?start=" + now.AddDays(1).ToString("yyyy-MM-dd") + "&end=" + now.AddDays(2).ToString("yyyy-MM-dd"));

                ArelAPI.DataStorage.saveData("planningToday", xmlToday);
                ArelAPI.DataStorage.saveData("planningTomorrow", xmlTomorrow);
            }        
           
                DrawPlanning(grid);
                DrawPlanning(grid2);
                await WritePlanningAsync(ArelAPI.DataStorage.getData("planningToday"), grid);
                await WritePlanningAsync(ArelAPI.DataStorage.getData("planningTomorrow"), grid2);

                FirstGrid.Visibility = Visibility.Visible;
                SecondGrid.Visibility = Visibility.Visible;
                LoadingIndicator.Visibility = Visibility.Collapsed;

                UpdateLayout();
            


        }

        /*
         * Récupère depuis un XML de l'API les informations des cours, et les écrit dans la grille de journée spécifiée.  
         */
        private async Task<bool> WritePlanningAsync(string xml, Grid planningGrid)
        {

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable

            var tasks = new List<Task>();

            foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)//on parcours tout les noeuds en parallèle
            {
                tasks.Add(ComputeCoursAsync(node, planningGrid)); 
            }

            //LA VITESSE
            await Task.WhenAll(tasks);

            return true;
        }

        //Récupère les données du noeud XML du cours et appelle ajoutCours pour le dessiner à l'écran
        private async Task<bool> ComputeCoursAsync(System.Xml.XmlNode node, Grid planningGrid)
        {

            string prof = "";
            string salle = "";
            string matiere = "";
            string matiere2 = "";
            string debut = "";
            string fin = "";
            string couleur = "";

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

            string idRel = node.ChildNodes[2].InnerText;
            string idProf = node.ChildNodes[3].InnerText;
            string profName = node.ChildNodes[4].InnerText;

            //Récup nom complet prof et rel...si on a accès à l'API parce que je les sauvegarde pas dans les données de l'appli
            Boolean isOnline = await API.IsOnlineAsync();
            if (isOnline)
            {
                string xmlr = await API.GetInfoAsync("/api/rels/" + idRel);
                matiere2 = API.getRelName(xmlr, matiere);
                matiere = "(" + matiere + ")";

                string xmlj = await API.GetInfoAsync("/api/users/" + idProf);
                profName = API.GetUserFullName(xmlj, profName);
            }

            if (prof != "" && matiere != "" && debut != "" && fin != "" && couleur != "" && salle != "")
                AddCours(matiere2, matiere, prof, salle, debut, fin, couleur, planningGrid);

            return true;

        }

        /*
         * Ajoute le cours détaillé à la grille de planning spécifiée.
         * Il y a actuellement deux grilles sur la page, une pour chaque jour.
         * 4 lignes dispos par cours (un peu crado dans l'implémentation mais bon)
         */
        private void AddCours(string line1, string line2, string line3, string line4, string heureDebut, string heureFin, string couleur, Grid planningGrid)
        {
            DateTime now = DateTime.Now;

            DateTime dt = Convert.ToDateTime(heureDebut);
            DateTime dt2 = Convert.ToDateTime(heureFin);
            
            int col = (int) dt.DayOfWeek - 1;

            int ligneDeb = ((dt.Hour - 8) * 4) + 1;
            int ligneFin = ((dt2.Hour - 8) * 4);
            if (ligneDeb > 0 && col < 6)
            {
                ligneDeb += (dt.Minute / 15);
                ligneFin += (dt2.Minute / 15);

                TextBlock block1 = CreateCoursTextBlock(line1);
                TextBlock block2 = CreateCoursTextBlock(line2);
                TextBlock block3 = CreateCoursTextBlock(line3);
                TextBlock block4 = CreateCoursTextBlock(line4);

                for (int i = ligneDeb; i <= ligneFin; i++)
                {
                    StackPanel macase = new StackPanel();

                    if (i == ligneDeb)
                        macase.Children.Add(block1);

                    if (i == ligneDeb + 1)
                        macase.Children.Add(block2);

                    if (i == ligneDeb + 2)
                        macase.Children.Add(block3);

                    if (i == ligneDeb + 3)
                        macase.Children.Add(block4);

                    macase.Background = HexToColor(couleur);

                    planningGrid.Children.Add(macase);
                    Grid.SetColumn(macase, col);
                    Grid.SetRow(macase, i);
                }
            }

        }

        private TextBlock CreateCoursTextBlock(string text)
        {
            return new TextBlock()
            {
                Text = text,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.Black)
            };
        }

        /*
         * Dessine sur la grille spécifiée le squelette du planning (heures et lignes) 
         */
        private void DrawPlanning(Grid planningGrid)
        {

            for (int j = 1; j < 41; j++)
            {
                TextBlock heure = new TextBlock();
                heure.FontSize = 13;
                int jj = (j / 4);
                if (j == (4 * jj) || j == 1)
                    heure.Text = (8 + jj).ToString() + "h ";
                planningGrid.Children.Add(heure);

                Grid.SetColumn(heure, 0);
                Grid.SetRow(heure, j);

                //Remplissage de la colonne
                StackPanel macase = new StackPanel();

                if (j == (4 * jj))
                    macase.BorderThickness = new Thickness(1,0,1,1);
                else
                    macase.BorderThickness = new Thickness(1, 0, 1, 0);

                if (j == 1)
                    macase.BorderThickness = new Thickness(1, 1, 1, 0);

                macase.BorderBrush = new SolidColorBrush(Colors.LightGray);

                planningGrid.Children.Add(macase);
                Grid.SetColumn(macase, 1);
                Grid.SetRow(macase, j);
                
            }

        }

        //-------------------- convertisseur hexa -> rgb ---------------------------------------------

        public SolidColorBrush HexToColor(String hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte r = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));

            SolidColorBrush myBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
            return myBrush;
        }

    }
}
