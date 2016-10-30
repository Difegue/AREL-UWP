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
    public sealed partial class About : Page { 
        public int angle = 0;
        private int mult = 1;

        public About()
        {
            this.InitializeComponent();

            if (ArelAPI.DataStorage.getData("themePref") == "Dark")
                AboutView.RequestedTheme = ElementTheme.Dark;
            else
                AboutView.RequestedTheme = ElementTheme.Light;

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            dispatcherTimer.Start();

        }

        void dispatcherTimer_Tick(object sender, object e)
        {
            angle += mult;
            RotateTransform rt = new RotateTransform();
            rt.Angle = angle;

            logo.RenderTransform = rt;
        }

        private void speedUp(object sender, PointerRoutedEventArgs e)
        {
            mult++;
        }

        private void cursorHand(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor =
                new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
        }

        private void cursorArrow(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor =
                new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
        }
    }
    
}
