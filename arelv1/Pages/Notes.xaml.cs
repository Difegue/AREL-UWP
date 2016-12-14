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

    public sealed partial class Notes : Page
    {
        private ArelAPI.Connector API = new ArelAPI.Connector();
        //Utilisé pour le tri initial
        private Dictionary<string, Module> modules = new Dictionary<string, Module>();
        //Utilisé pour le data binding XAML après
        private Semestre ueSem1 = new Semestre();
        private Semestre ueSem2 = new Semestre();

        public Notes()
        {
            this.InitializeComponent();
            initPage();

        }

        private async void initPage()
        {
            Boolean isOnline = await API.IsOnlineAsync();

            if (isOnline)
            {
                string notesXml = await API.GetInfoAsync("/api/me/marks");
                ArelAPI.DataStorage.saveData("notes", notesXml);
                buildNotes(notesXml);
            }
            else if (ArelAPI.DataStorage.isset("notes"))
            {
                string notesXml = ArelAPI.DataStorage.getData("notes");
                buildNotes(notesXml);
            }
            else
            {
                NoInternetSplash.Visibility = Visibility.Visible;
            }
        }

        private void initPage(object sender, RoutedEventArgs e)
        {
            initPage();
        }

        private void buildNotes(string notesXml)
        {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(notesXml);//chargement de la variable

            //Structure de l'xml renvoyé par l'API: Liste de sites ayant chacun un attribut "id" et un innerText correspondand au nom du campus
            foreach (System.Xml.XmlNode note in doc.FirstChild.ChildNodes)
            {
                /*
                 * Structure standard d'un élément Note dans le retour de l'API :
                 * <marks>
                 *  <id>
                 *      <studentArelId>17779</studentArelId>
                 *      <programmeId>6488</programmeId>
                 *      <semestreId>262</semestreId>
                 *      <moduleId>18</moduleId>
                 *      <ueId>823</ueId>
                 *      <epreuveId>3890</epreuveId>
                 *  </id>
                 *  <label>Analyse Financiere</label>
                 *  <moduleName>ING2</moduleName>
                 *  <mark>0.0</mark>
                 *  <coefficient>1.0</coefficient>
                 *  <testName>Examen 1ère session</testName>
                 *  <typeEpreuve>Examen</typeEpreuve>
                 * </marks>
                 */

                //TODO: A refaire avec node.GetElementsByTagName
                Note n = new Note();
                n.id = Int32.Parse(note.ChildNodes[0].ChildNodes[5].InnerText);
                n.labelNote = note.ChildNodes[5].InnerText;
                n.value = float.Parse(note.ChildNodes[3].InnerText, System.Globalization.CultureInfo.InvariantCulture);
                n.coef = float.Parse(note.ChildNodes[4].InnerText, System.Globalization.CultureInfo.InvariantCulture);

                string moduleId = note.ChildNodes[0].ChildNodes[3].InnerText;
                string semestreId = note.ChildNodes[0].ChildNodes[2].InnerText;
                Module m = null;

                //Est-ce qu'on a déjà croisé ce module ?
                //On utilise moduleId+semestreId en clé pour ne pas qu'un module concatène les notes des 2 semestres à la fois 
                if (modules.ContainsKey(moduleId+semestreId))
                {
                    m = (Module)modules[moduleId + semestreId];
                    modules.Remove(moduleId + semestreId);
                }
                else
                {
                    //On crée le module
                    m = new Module();
                    m.id = moduleId;
                    m.labelModule = note.ChildNodes[1].InnerText;
                    m.notes = new List<Note>();
                    m.idSemestre = semestreId;
                    m.idUE = note.ChildNodes[0].ChildNodes[4].InnerText;
                }

                //On ajoute la note au module avant de le restocker dans le dictionnaire
                m.notes.Add(n);
                modules.Add(moduleId + semestreId, m);

            }

            //On a récupéré tous les modules, chacun ayant toutes les notes le concernant
            //On sépare maintenant par UE et par Semestre. Y'aurait sûrement moyen de faire ça en LINQ mais je m'y connais pas assez haha mdr
            //Chaque dictionnaire de semestre contient une clé par UE. Cette clé contient la liste des modules de l'UE.
            foreach (String key in modules.Keys)
            {
                Module m = modules[key];

                if (ueSem1.id == null) //Premier arrivé premier servi
                    ueSem1.id = m.idSemestre;

                if (m.idSemestre == ueSem1.id)
                    //On ajoute dans le semestre 1, semestre 2 sinon
                    AddModuleToUE(m, ueSem1);
                else
                {
                    AddModuleToUE(m, ueSem2);
                    ueSem2.id = m.idSemestre;
                }

            }

            //Comparer les IDSemestre des deux dicos pour voir lequel est le vrai semestre 1 (IDSem1 < IDSem2)
            //Sauf si un des 2 est null auquel cas seul l'autre est Semestre 1
            if (ueSem1.id == null || ueSem2.id == null)
            {
                if (ueSem1.id == null)
                    ueSem1 = ueSem2;

                if (ueSem2.id == null)
                    ueSem2 = ueSem1;

                //On vire le pivot du semestre 2
                pivotSem2.Visibility = Visibility.Collapsed;

                //Si le semestre 1 est vide aussi, il n'y a juste pas de notes -> On affiche le splashscreen associé.
                if (ueSem1.id == null)
                {
                    NoNoteSplash.Visibility = Visibility.Visible;
                }

            }
            else
            if (Int32.Parse(ueSem2.id) < Int32.Parse(ueSem1.id)) //Si les semestres étaient inversés, on les remet à l'endroit.
            {
                Semestre tmp = ueSem1;
                ueSem1 = ueSem2;
                ueSem2 = tmp;
            }

            ueSem1.sortUEs();
            //ueSem2.sortUEs();

            if (NoNoteSplash.Visibility != Visibility.Visible)
            {
                semestresPivot.Visibility = Visibility.Visible;
            }

            //Fiou, enfin fini. Le XAML s'occupe de construire la vue des notes via data binding.
            this.Bindings.Update();
            UpdateLayout();
        }


        private void AddModuleToUE(Module m, Semestre s)
        {
            UE u = s.getUE(m.idUE);

            //On regarde si on a déjà cet UE 
            if (u == null)
                u = new UE(m.idUE);

            u.modules.Add(m);
            s.listUE.Add(u);
        }

        private void showSem1(object sender, PointerRoutedEventArgs e)
        {
            sem1Liste.Visibility = Visibility.Visible;
            sem2Liste.Visibility = Visibility.Collapsed;
            sem2Selected.IsActive = false;
            sem1Selected.IsActive = true;
            UpdateLayout();
        }

        private void showSem2(object sender, PointerRoutedEventArgs e)
        {
            sem1Liste.Visibility = Visibility.Collapsed;
            sem2Liste.Visibility = Visibility.Visible;
            sem1Selected.IsActive = false;
            sem2Selected.IsActive = true;
            UpdateLayout();
        }
    }


    

}
