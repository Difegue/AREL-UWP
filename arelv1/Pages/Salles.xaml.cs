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

            //API.saveData("salles", 

            //On récupère d'abord la liste des campus dispo
            string campusXML = API.getInfo("api/campus/sites");
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(campusXML);//chargement de la variable

            //Structure de l'xml renvoyé par l'API: Liste de sites ayant chacun un attribut "id" et un innerText correspondand au nom du campus
            foreach (System.Xml.XmlNode site in doc.FirstChild.ChildNodes)
            {
                campusList.Add(new Campus(site.Attributes[0].Value, site.InnerText));
            }
            //On met le premier campus de la liste en valeur par défaut 
            SelectedComboBoxOption = campusList[0];


        }

        private void writeSalle(object sender, SelectionChangedEventArgs e)
        {
            //On vide la liste des salles qu'on a 
            salleList.Clear();

            //On récupère le campus sélectionné
            Campus c = (Campus)campusSelection.SelectedItem;
            //On récupère les salles du campus sélectionné
            String xmlSalles = API.getInfo("api/campus/rooms?siteId="+c.getId());

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

                if (nom !="???" && nom!="DAUPHINE" && nom!="ENSEA" && nom!="GEM") 
                    salleList.Add(new Salle(nom, desc, bookable, tbl, cap));

            }

        }
    }

    //Mini classe pour faciliter la sélection de campus dans la combobox
    public class Campus
    {
        private string id;
        private string label;

        public Campus(string id, string label)
        {
            this.id = id;
            this.label = label;
        }

        public override string ToString()
        { return label; }

        public string getId()
        { return id; }
    }

    //Idem pour les salles
    public class Salle
    {
        public string nom;
        public string desc;
        public bool dispo;
        public string tbl;
        public string cap;

        public Salle(string n, string d, bool di, string t, string c)
        {
            nom = n;
            desc = d;
            dispo = di;
            tbl = t;
            cap = c;
        }

    }

    //Convertisseur à utiliser en XAML pour obtenir couleur et type d'icône à partir du boolean dispo de la classe Salle.
    public class DispoToIcon: IValueConverter
    {
        // Define the Convert method to convert the boolean to a Symbol or a color string, depending on the type required.
        public object Convert(object value, Type targetType,
            object parameter, string language)
        {
            if (targetType == typeof(Symbol))
            {
                if ((bool)value)
                    return Symbol.Emoji2;
                else
                    return Symbol.Cancel;
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
