using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace arelv1.Pages
{
    public sealed partial class Settings : Page
    {
        private ArelAPI.Connector API = new ArelAPI.Connector();
        private bool taskRegistered = false;
        private bool firstLaunch = false;
        private string taskName;

        public Settings()
        {
            this.InitializeComponent();
            string themeSetting = ArelAPI.DataStorage.getData("themePref");

            //Preset des toggles
            if ( themeSetting == "Dark")
            {
                ThemeSwitch.IsOn = true;
            }

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

            ArelAPI.DataStorage.saveData("backgroundTask", taskRegistered.ToString());
            BackgroundSyncSwitch.IsOn = taskRegistered;

            UpdateLayout();
        }

        private void AutoSync(object sender, RoutedEventArgs e)
        {
            if (firstLaunch) //vérifie si l'appel à cette fonction vient du IsOn de l'initialisation
            {
                firstLaunch = false;
                return;
            }
            
            if (taskRegistered)
            {
                ArelAPI.DataStorage.saveData("backgroundTask", "false");
                taskRegistered = false;
            }
            else
            {
                //On vérifie si la tâche n'est pas déjà enregistrée
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
                ArelAPI.DataStorage.saveData("backgroundTask", "true");
                taskRegistered = true;

                ManualSync(sender, e);
            }
        }

        private async void ManualSync(object sender, RoutedEventArgs e)
        {
            //On montre un spinner pour l'amusement
            SpinnerSync.IsActive = true;

            //update manuelle: On chope les cours des 2 dernières + des 2 prochaines semaines
            API.UpdateWindowsCalendar(DateTime.Now.AddDays(-14).ToString("yyyy-MM-dd"), DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"), API.GetUserFullName(ArelAPI.DataStorage.getData("user"), "Mon Planning AREL"));

            //On montre le calendrier
            await Windows.ApplicationModel.Appointments.AppointmentManager.ShowTimeFrameAsync(DateTime.Now, new TimeSpan(125, 0, 0));
            SpinnerSync.IsActive = false;
        }


        private void changeTheme(object sender, RoutedEventArgs e)
        {

            if (ThemeSwitch.IsOn)
            {
                ArelAPI.DataStorage.saveData("themePref", "Dark");
            }
            else
            {
                ArelAPI.DataStorage.saveData("themePref", "Light");
            }
      
        }

        private async void aboutClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var currentAV = ApplicationView.GetForCurrentView();
            var newAV = CoreApplication.CreateNewView();
            await newAV.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            async () =>
                            {
                                var newWindow = Window.Current;
                                var newAppView = ApplicationView.GetForCurrentView();
                                newAppView.Title = "Licence Information";

                                var frame = new Frame();
                                frame.Navigate(typeof(About), null);
                                newWindow.Content = frame;
                                newWindow.Activate();

                                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
                                    newAppView.Id,
                                    ViewSizePreference.UseMinimum,
                                    currentAV.Id,
                                    ViewSizePreference.UseMinimum);
                            });
        }
    }
}
