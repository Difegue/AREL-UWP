using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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


namespace arelv1.Pages
{

    public sealed partial class Salles: Page
    {
        //Collections utilisées en data binding sur l'UI XAML
        private ObservableCollection<Campus> campusList= new ObservableCollection<Campus>();
        private ObservableCollection<Salle> salleList = new ObservableCollection<Salle>();

        private ArelAPI.Connector API = new ArelAPI.Connector();
        private Campus SelectedComboBoxOption;

        public Salles()
        {
            this.InitializeComponent();

            //Vérifier si des campuses sont enregistrés : si oui, on itère dessus et on wipe les salles enregistrées
            if (ArelAPI.DataStorage.isset("campuses"))
            {
                string campusXML = ArelAPI.DataStorage.getData("campuses");
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.LoadXml(campusXML);//chargement de la variable

                foreach (System.Xml.XmlNode site in doc.FirstChild.ChildNodes)
                {
                    ArelAPI.DataStorage.saveData("salles" + site.Attributes[0].Value, "");
                }
            }

            getSalles(null,null);
            UpdateLayout();

        }

        private void searchSalle(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            //Refresh salleList
            writeSalle(sender, null);

            //Get the string searched by the user
            string search = salleSearchBox.Text;

            //Filter salleList and return the result as the databinding
            List<Salle> searchResult = salleList.Where(item => item.nom.StartsWith(search)).ToList();
            salleList.Clear();
            
            foreach (Salle s in searchResult)
            {
                salleList.Add(s);
            }

            UpdateLayout();
        }

        private async void getSalles(object sender, RoutedEventArgs e)
        {
            string campusXML;

            //On récupère d'abord la liste des campus dispo, fraîche de l'API 
            Boolean isOnline = await API.IsOnlineAsync();
            if (isOnline)
            {
                salleGrid.Visibility = Visibility.Visible;
                NoInternetSplash.Visibility = Visibility.Collapsed;

                campusXML = await API.GetInfoAsync("/api/campus/sites");
                ArelAPI.DataStorage.saveData("campuses", campusXML);

                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
                doc.LoadXml(campusXML);//chargement de la variable

                //Structure de l'xml renvoyé par l'API: Liste de sites ayant chacun un attribut "id" et un innerText correspondand au nom du campus
                foreach (System.Xml.XmlNode site in doc.FirstChild.ChildNodes)
                {
                    campusList.Add(new Campus(site.Attributes[0].Value, site.InnerText));
                }

                //On met le premier campus de la liste en valeur par défaut, sauf si l'utilisateur a une préférence
                SelectedComboBoxOption = null;
                if (ArelAPI.DataStorage.isset("favCampus"))
                {
                    string idFav = ArelAPI.DataStorage.getData("favCampus");
                    foreach (Campus c in campusList)
                        if (c.getId() == idFav)
                            SelectedComboBoxOption = c;
                }

                if (SelectedComboBoxOption == null)
                    SelectedComboBoxOption = campusList[0];

                this.Bindings.Update();
                UpdateLayout();
                
            }
            else //Aucun intérêt à voir les salles dispos si on a pas internet pour avoir des datas à jour, on affiche le splash d'erreur
            {
                //API.renewAccessToken();
                salleGrid.Visibility = Visibility.Collapsed;
                NoInternetSplash.Visibility = Visibility.Visible;
                UpdateLayout();
            }
        }

        private async void writeSalle(object sender, SelectionChangedEventArgs e)
        {
            //On vide la liste des salles qu'on a 
            salleList.Clear();

            //On récupère le campus sélectionné et on le sauvegarde en tant que pref. de l'utilisateur
            Campus c = (Campus)campusSelection.SelectedItem;

            ArelAPI.DataStorage.saveData("favCampus", c.getId());

            //On récupère les salles du campus sélectionné -- On regarde d'abord si il y a un cache pour la session en cours, sinon on récupère de l'API.

            String xmlSalles = ArelAPI.DataStorage.getData("salles" + c.getId());

            if( xmlSalles == null || xmlSalles == "" || xmlSalles == "\r\n")
            {
                Boolean isOnline = await API.IsOnlineAsync();
                if (isOnline)
                {
                    xmlSalles = await API.GetInfoAsync("/api/campus/rooms?siteId=" + c.getId());
                    //On sauvegarde les salles pour que la recherche ne retape pas dans l'API à chaque fois
                    ArelAPI.DataStorage.saveData("salles" + c.getId(), xmlSalles);
                }
                else
                {
                    salleGrid.Visibility = Visibility.Collapsed;
                    NoInternetSplash.Visibility = Visibility.Visible;
                    UpdateLayout();
                }

                
            }

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xmlSalles);//chargement de la variable

            //On récupère les salles et on peuple notre tableau de salles
            foreach (System.Xml.XmlNode room in doc.GetElementsByTagName("room"))
            {
                /*Stucture xml d'une salle donnée par l'api :
                 * <label>
                 * <bookable>
                 * <capacity>
                 * <tables>
                 * <assignedTo>
                 * <description>
                 * <connected>
                 * <campusId>
                 */
                string nom = room.ChildNodes[0].InnerText;

                string desc;
                if (room.ChildNodes[5].InnerText == "Pas de description") //Pourquoi lorsque y'a pas de description ils le précisent,
                    desc = room.ChildNodes[4].InnerText; //alors que quand la salle est assignée à personne le tag est juste vide ?
                else
                    desc = room.ChildNodes[5].InnerText; //Un peu de cohérence bordel

                bool bookable = (room.ChildNodes[1].InnerText == "true");
                string tbl = room.ChildNodes[3].InnerText;
                string cap = room.ChildNodes[2].InnerText;

                if (nom !="???") 
                    salleList.Add(new Salle(nom, desc, bookable, tbl, cap));

            }

        }

    }


    //Convertisseur à utiliser en XAML pour obtenir couleur et type d'icône à partir du boolean dispo de la classe Salle.
    public class DispoToIcon : IValueConverter
    {
        // Define the Convert method to convert the boolean to a Symbol or a color string, depending on the type required.
        public object Convert(object value, Type targetType,
            object parameter, string language)
        {
            if (targetType == typeof(string))
            {
                if ((bool)value)
                    return "\uEC61";
                else
                    return "\uEB90";
            }

            if (targetType == typeof(Brush))
            {
                if ((bool)value)
                    return new SolidColorBrush(Colors.DarkGreen);
                else
                    return new SolidColorBrush(Colors.DarkRed);
            }

            return "mdr";

        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


}
