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


namespace arelv1.Pages
{
    public sealed partial class Salles: Page
    {
        private ArelApi API = new ArelApi();

        public Salles()
        {
            this.InitializeComponent();

            writeSalle(API.getData("salles"));

        }

        private void writeSalle(string xml)
        {
            //List<StackPanel> salles = new List<StackPanel>();
            //List<String> salles = new List<String>();
            string aff;
            string libre;
            int i = 0;
            TextBlock nomBlock;
            TextBlock libreBlock;
            StackPanel macase;
            SolidColorBrush color;

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();//creation d'une instance xml
            doc.LoadXml(xml);//chargement de la variable

            //salles.Add("nom de salle".PadRight(15, '.') + "libre?".PadLeft(15,'.'));
            foreach (System.Xml.XmlNode room in doc.GetElementsByTagName("room"))
            {
                aff = "";
                libre = "";
                color = new SolidColorBrush(Colors.Red);
                foreach (System.Xml.XmlNode label in room)
                {
                    if (label.Name == "label")
                    {
                        aff = label.InnerText + ":";
                    }
                    if (label.Name == "bookable")
                    {
                        if (label.InnerText == "true")
                        {
                            libre = " ok";
                            color = new SolidColorBrush(Colors.Green);
                        }
                        else
                        {
                            libre = " non";
                            color = new SolidColorBrush(Colors.Red);
                        }

                    }
                }
                RowDefinition Row = new RowDefinition();
                Row.Height = new GridLength(35);
                salles.RowDefinitions.Add(Row);

                nomBlock = new TextBlock();
                libreBlock = new TextBlock();
                macase = new StackPanel();

                nomBlock.Text = aff;
                nomBlock.Foreground = new SolidColorBrush(Colors.SaddleBrown);
                nomBlock.HorizontalAlignment = HorizontalAlignment.Center;

                libreBlock.Text = libre;
                libreBlock.Foreground = color;
                libreBlock.HorizontalAlignment = HorizontalAlignment.Center;

                salles.Children.Add(nomBlock);
                Grid.SetColumn(nomBlock, 0);
                Grid.SetRow(nomBlock, i);

                salles.Children.Add(libreBlock);
                Grid.SetColumn(libreBlock, 1);
                Grid.SetRow(libreBlock, i);


                i++;

                //macase.Children.Add(nomBlock);
                //macase.Children.Add(libreBlock);
                //macase.HorizontalAlignment = HorizontalAlignment.Center;

                //salles.Add(macase);

                //salles.Add(aff.PadRight(30 - aff.Length, '.') + libre);
                //salles.Add (String.Format("{0, 5} {1, 10}", aff, libre));*/
            }

        }
    }
}
