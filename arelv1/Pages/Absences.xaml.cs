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

        public Absences()
        {
            initPage();
        }

        private void initPage()
        {
            if (API.isOnline())
            {
                string absencesXml = API.getInfo("/api/me/absences");
                API.saveData("absences", absencesXml);
                buildAbsences(absencesXml);
            }
            else if (API.isset("absences"))
            {
                string absencesXml = API.getData("absences");
                buildAbsences(absencesXml);
            }
            else
            {
                //semestresPivot.Visibility = Visibility.Collapsed;
                NoInternetSplash.Visibility = Visibility.Visible;
            }
        }

        private void buildAbsences(string xml)
        {

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable

            //Structure de l'xml renvoyé par l'API: Liste d'absences, chacune contenant un ID vers le slot concerné d'emploi du temps
            foreach (System.Xml.XmlNode absence in doc.FirstChild.ChildNodes)
            {
                string idSlot = absence.ChildNodes[1].InnerText;
                string slotInfo = API.getInfo("/api/plannings/slots/" + idSlot); //La seconde childnode contient le slotId
                System.Xml.XmlDocument doc2 = new System.Xml.XmlDocument();
                doc2.LoadXml(slotInfo);



            }

        }

        private void initPage(object sender, RoutedEventArgs e)
        {
            initPage();
        }

    }
}
