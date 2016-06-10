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
        private bool taskRegistered = false;
        private bool firstLaunch = false;
        private string taskName;

        public Planning()
        {
            this.InitializeComponent();

            dateJour.Text = "Nous sommes le " + getDayStr(now, 0);

            if (API.isOnline())
                updatePlanning(0); //Stocke le planning du jour dans la clé "planning" de l'appli si on a internet

            DrawPlanning();
            writePlanning(API.getData("planning"));
            UpdateLayout();

            //Etat de la tâche de synch du planning pour cet utilisateur
            taskName = "ARELSyncPlanningTask";
            
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    taskRegistered = true;
                    firstLaunch = true; //Pour permettre au premier appel de toggled - qui vient dès le démarrage de l'appli vu qu'on met isOn = true si y'a déjà un background event - de passer sans désactiver le call et foutre le bordel, on met ce bool en vérif
                    break;
                }
            }

            API.saveData("backgroundTask", taskRegistered.ToString());
            BackgroundSyncSwitch.IsOn = taskRegistered;

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

            return new string(a1) + new string(a2);
        }



        private async void ManualSync(object sender, RoutedEventArgs e)
        {
            //On montre un spinner pour l'amusement
            SpinnerSync.IsActive = true;

            //update manuelle: On chope les cours des 2 dernières + des 2 prochaines semaines
            API.updateWindowsCalendar(DateTime.Now.AddDays(-14).ToString("yyyy-MM-dd"), DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"), API.getUserFullName(API.getData("user")));

            //On montre le calendrier
            await Windows.ApplicationModel.Appointments.AppointmentManager.ShowTimeFrameAsync(DateTime.Now, new TimeSpan(125, 0, 0));
            SpinnerSync.IsActive = false;
        }

        private void AutoSync(object sender, RoutedEventArgs e)
        {
            if (firstLaunch)
                firstLaunch = false;
            else
                if (taskRegistered)
                {
                    API.saveData("backgroundTask", "false");
                    taskRegistered = false;
                }
                else
                {
                    //On vétifie si la tâche n'est pas déjà enregistrée
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (task.Value.Name == taskName)
                        {
                            taskRegistered = true;
                            return;
                        }
                    }

                    var builder = new BackgroundTaskBuilder();
                    TimeTrigger hourlyTrigger = new TimeTrigger(120, false); //On rafraîchit le planning toutes les 2 heures.

                    builder.Name = taskName;
                    builder.TaskEntryPoint = "SyncTask.ARELPlanningBackgroundTask";
                    builder.SetTrigger(hourlyTrigger);
                    builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));

                    builder.Register();
                    API.saveData("backgroundTask", "true");
                    taskRegistered = true;

                    ManualSync(sender,e);
                }
        }


        /*
         * Les fonctions ci-dessous concernent le dessin de l'EDT sur le panel de gauche, c'est un peu legacy
         */

        private void updatePlanning(int daysExtra)
        {
            now = now.AddDays(daysExtra);

            API.saveData("planning", API.getInfo("api/planning/slots?start=" + now.ToString("yyyy-MM-dd") + "&end=" + now.AddDays(1).ToString("yyyy-MM-dd")));
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
                    
                    macase.Background = HexToColor(couleur);

                    //bugfix chelou mais si on en arrive là c'est que y'a vraiment eu un souci
                    if (col < 0)
                        col = 0;

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

            lundi = weekToDate(Convert.ToInt32(week1), 2016, "lundi");


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

                string idProf = node.ChildNodes[3].InnerText;
                string profName = node.ChildNodes[4].InnerText;

                //Récup nom complet prof...si on a accès à l'API parce que je les sauvegarde pas dans les données de l'appli
                if (API.isOnline())
                { 
                string xmlj = API.getInfo("/api/users/" + idProf);
                profName = API.getUserFullName(xmlj);
                }

                if (prof != "" && matiere != "" && debut != "" && fin != "" && couleur != "" && salle != "")
                    ajoutCours(profName, debut, fin, matiere, couleur, lundi, salle);

            }



        }

        private void DrawPlanning()
        {

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

                    macase.BorderThickness = new Thickness(1, 1, 1, 1);
                    macase.BorderBrush = new SolidColorBrush(Colors.LightGray);

                    grid.Children.Add(macase);
                    Grid.SetColumn(macase, i);
                    Grid.SetRow(macase, j);
                }
            }

        }

        //-------------------- convertisseur n° semaine en date --------------------------------------

        public DateTime weekToDate(int week, int year, string day)
        {
            //Jour int ISO
            Dictionary<string, int> dicDays = new Dictionary<string, int>()
            {
                {"lundi", 1 },
                {"mardi", 2 },
                {"mercredi", 3},
                {"jeudi", 4 },
                {"vendredi", 5 },
                {"samedi", 6 },
                {"dimanche", 7 }
            };




            DateTime value = new DateTime(year, 1, 1).AddDays(7 * week);

            int daysToAdd = dicDays[day.ToLower()];

            // On contrôle si l'année commence après jeudi si oui on décale d'une semaine.
            if ((int)new DateTime(value.Year, 1, 1).DayOfWeek < 5)
            {
                daysToAdd -= (int)value.DayOfWeek + 7;
            }
            else
            {
                daysToAdd -= (int)value.DayOfWeek;
            }

            return value.AddDays(daysToAdd);
        }


        //-------------------- convertisseur hexa -> rgb ---------------------------------------------

        public SolidColorBrush HexToColor(String hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte r = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            //byte b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
            SolidColorBrush myBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
            return myBrush;
        }

    }
}
