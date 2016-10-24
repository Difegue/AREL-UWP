using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arelv1.Pages
{
    //Classes représentant divers éléments de l'API pour un Data Binding facile côté XAML.

    public class Note
    {
        public string labelNote;
        public float value;
        public float coef;
        public int id;

    }
    public class Absence
    {
        public DateTime startDate;
        public DateTime endDate;
        public String labelMatiere;

        public String day;
        public String endHour;
        public String startHour;


        public Absence(DateTime startDate, DateTime endDate, string labelMatiere)
        {
            this.startDate = startDate;
            this.endDate = endDate;
            this.labelMatiere = labelMatiere;
            day = startDate.ToString("d");
            startHour = startDate.ToString("t");
            endHour = endDate.ToString("t");
        }
    }

    public class Module
    {
        public string labelModule;
        public string id;
        public string idUE;
        public string idSemestre;

        //Un module peut servir à regrouper des notes ou des absences, selon la page qui l'utilise.
        public List<Note> notes;
        public List<Absence> absences;

        //La moyenne du module. Inutilisable, vu qu'on ne peut pas faire la différence entre rattrapages et notes normales.
        public float calcMoyenne()
        {
            float totalNum = 0;
            float totalDen = 1;

            foreach (Note n in notes)
            {
                totalNum += n.value * n.coef;
                totalDen += n.coef;
            }

            return totalNum / totalDen;
        }
    }

    public class UE
    {
        public string id;
        public string labelUE;
        public List<Module> modules;

        public UE(string idu)
        {
            id = idu;
            labelUE = "UE n°" + id;
            modules = new List<Module>();
        }
    }

    public class Semestre
    {
        public string id;
        public List<UE> listUE;

        public Semestre()
        {
            listUE = new List<UE>();
            id = null;
        }

        //Récupère un UE de la liste avant de l'éffacer.
        public UE getUE(string id)
        {
            UE item = listUE.FirstOrDefault(u => u.id == id);
            listUE.Remove(item);

            return item;

        }

        //Trie les UEs par id croissant
        public void sortUEs()
        {
            listUE = listUE.OrderBy(ue => ue.id).ToList();
        }

    }

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

}
