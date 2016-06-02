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

            dateJour.Text = "Emploi du Temps AREL - " + getDayStr(now, 0);

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
         * Les fonctions ci-dessous concernent le dessin de l'EDT sur le panel de gauche
         */

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



        private void updatePlanning(int daysExtra)
        {
            now = now.AddDays(daysExtra);

            API.saveData("planning", API.getInfo("api/planning/slots?start=" + now.ToString("yyyy-MM-dd") + "&end=" + now.AddDays(1).ToString("yyyy-MM-dd")));
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            //Get new info
            updatePlanning(-1);
            grid.Children.Clear();
            DrawPlanning();
            writePlanning(API.getData("planning"));
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            updatePlanning(1);
            grid.Children.Clear();
            DrawPlanning();
            writePlanning(API.getData("planning"));
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




                    macase.Background = new SolidColorBrush(HexToColor(couleur));
                    //bugfix chelou je sais pas
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

                if (prof != "" && matiere != "" && debut != "" && fin != "" && couleur != "" && salle != "")
                    ajoutCours(prof, debut, fin, matiere, couleur, lundi, salle);

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

        public Color HexToColor(String hexString)
        {
            Color actColor;
            int r, g, b;
            r = 0;
            g = 0;
            b = 0;
            if ((hexString.StartsWith("#")) && (hexString.Length == 7))
            {
                r = HexToInt(hexString.Substring(1, 1)) * 16 + HexToInt(hexString.Substring(1, 1));
                g = HexToInt(hexString.Substring(3, 1)) * 16 + HexToInt(hexString.Substring(4, 1));
                b = HexToInt(hexString.Substring(5, 1)) * 16 + HexToInt(hexString.Substring(6, 1));

                actColor = Color.FromArgb(150, Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
            }
            else
            {
                actColor = Color.FromArgb(0, 0, 0, 0);
            }
            return actColor;
        }

        public int HexToInt(string s)
        {
            int res;
            switch (s)
            {
                case "1":
                    res = 1;
                    break;
                case "2":
                    res = 2;
                    break;
                case "3":
                    res = 3;
                    break;
                case "4":
                    res = 4;
                    break;
                case "5":
                    res = 5;
                    break;
                case "6":
                    res = 6;
                    break;
                case "7":
                    res = 7;
                    break;
                case "8":
                    res = 8;
                    break;
                case "9":
                    res = 9;
                    break;
                case "a":
                    res = 10;
                    break;
                case "b":
                    res = 11;
                    break;
                case "c":
                    res = 12;
                    break;
                case "d":
                    res = 13;
                    break;
                case "e":
                    res = 14;
                    break;
                case "f":
                    res = 15;
                    break;
                case "0":
                    res = 0;
                    break;
                default:
                    res = 0;
                    break;

            }
            return res;
        }

    }
}
