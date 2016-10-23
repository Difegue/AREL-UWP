using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class Absences : Page
    {
        private ArelAPI.Connector API = new ArelAPI.Connector();

        //Utilisé pour trier les absences par matière
        private Dictionary<string, Module> modules = new Dictionary<string, Module>();
        private List<Module> finalListModules;

        public Absences()
        {
            this.InitializeComponent();
            initPage();
        }

        private void initPage()
        {
            if (API.isOnline())
            {
                string absencesXml = API.getInfo("/api/me/absences");
                API.saveData("absences", absencesXml);

                //test string from resources.resw
                Windows.ApplicationModel.Resources.ResourceLoader loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                absencesXml = loader.GetString("AbsencesXmlTest");

                buildAbsences(absencesXml);
                finalListModules = modules.Values.ToList<Module>();
            }
            else if (API.isset("absences"))
            {
                string absencesXml = API.getData("absences");

                buildAbsences(absencesXml);
                finalListModules = modules.Values.ToList<Module>();
            }
            else
            {
                AbsenceStack.Visibility = Visibility.Collapsed;
                NoInternetSplash.Visibility = Visibility.Visible;
            }

            UpdateLayout();
        }

        private void buildAbsences(string xml)
        {

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable

            //Structure de l'xml renvoyé par l'API: Liste d'absences, chacune contenant un ID vers le slot concerné d'emploi du temps
            foreach (System.Xml.XmlNode absence in doc.FirstChild.ChildNodes)
            {
                string idSlot = absence.ChildNodes[1].InnerText;
                string slotInfo = API.getInfo("/api/planning/slots/" + idSlot); //La seconde childnode contient le slotId
                System.Xml.XmlDocument docAbsence = new System.Xml.XmlDocument();
                docAbsence.LoadXml(slotInfo);

                /*Structure d'un XML de slot planning :
                 * 
                 * <planningSlot id="875795">
                       <beginDate>2016-10-12 11:30:00</beginDate>
                       <endDate>2016-10-12 13:00:00</endDate>
                       <label>LV1-</label>
                       <lectureType>CT</lectureType>
                       <relId>206</relId>
                       <programId>6488</programId>
                       <siteId>1991</siteId>
                       <affectations>
                        (ids des profs/élèves)
                       </affectations>
                   </planningSlot>
                */

                //On chope le rel (la matière) correspondant à ce slot via le RelId
                string relId = docAbsence.GetElementsByTagName("relId")[0].InnerText;
                string relInfo = API.getInfo("/api/rels/" + relId);
                System.Xml.XmlDocument docRel = new System.Xml.XmlDocument();
                docRel.LoadXml(relInfo);

                string labelMatiere = docRel.GetElementsByTagName("label")[0].InnerText;

                DateTime startDate = new DateTime();
                DateTime.TryParse(docAbsence.GetElementsByTagName("beginDate")[0].InnerText, out startDate);

                DateTime endDate = new DateTime();
                DateTime.TryParse(docAbsence.GetElementsByTagName("endDate")[0].InnerText, out endDate);

                Absence a = new Absence(startDate, endDate, labelMatiere);

                //Add absence to rel, create module objet for rel if it doesn't exist.
                if (modules.ContainsKey(relId))
                {
                    modules[relId].absences.Add(a);
                }
                else
                {
                    Module m = new Module();
                    m.id = relId;
                    m.absences = new List<Absence>();
                    m.absences.Add(a);
                    m.labelModule = labelMatiere;
                    modules[relId] = m;
                }

            }

            if (modules.Count == 0)
            {
                AbsenceStack.Visibility = Visibility.Collapsed;
                NoAbsenceSplash.Visibility = Visibility.Visible;
            }

        }

        private void initPage(object sender, RoutedEventArgs e)
        {
            initPage();
        }

    }

}
