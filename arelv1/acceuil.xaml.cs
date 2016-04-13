using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arelv1
{
    public sealed partial class acceuil
    {
        public acceuil()
        {
            InitializeComponent();
            //var messageDialog = new Windows.UI.Popups.MessageDialog("toto");
            //rootPage.NotifyUser("toto");
        }

        private void HamburgerButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            hamburger.IsPaneOpen = !hamburger.IsPaneOpen;
        }
    }
}
