using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using NotificationsExtensions.Toasts;
using Windows.UI.Notifications;

namespace SyncTask
{
    public sealed class ARELPlanningBackgroundTask : IBackgroundTask
    {
        /*
         * If you run any asynchronous code in your background task, then your background task needs to use a deferral. 
         * If you don't use a deferral, then the background task process can terminate unexpectedly if the Run method 
         * completes before your asynchronous method call has completed.
         */

        private void Show(ToastContent content)
        {
            ToastNotification t = new ToastNotification(content.GetXml());
            t.ExpirationTime = DateTime.Now.AddDays(2);
            t.Tag = "1";
            t.Group = "ARELNotif";

            if (ToastNotificationManager.History.GetHistory().Count>0) //Si la notif est toujours dans l'action center, on ne met pas de popup
                t.SuppressPopup = true; //Faudrait pas faire chier l'utilisateur non plus
            ToastNotificationManager.CreateToastNotifier().Show(t);
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            BackgroundTaskDeferral _deferral = taskInstance.GetDeferral();
            ArelAPI.Connector API = new ArelAPI.Connector();

            Boolean canUpdate = true;
            Boolean isOnline = await API.IsOnlineAsync();

            if (!isOnline) //Si le token n'est plus valide, on le rafraîchit avec le refreshToken
            {
                bool isReLogged = await API.RenewAccessTokenAsync();
                if (!isReLogged) //Si on peut rafraîchir le jeton, on continue, sinon on notifie l'utilisateur qu'il doit ré-entrer ses logins
                {
                    Show(new ToastContent()
                    {
                        Scenario = ToastScenario.Default,

                        Visual = new ToastVisual()
                        {
                            TitleText = new ToastText() { Text = "AREL - Synchronisation Planning" },
                            BodyTextLine1 = new ToastText() { Text = "Vos identifiants ont expirés." },
                            BodyTextLine2 = new ToastText() { Text = "Reconnectez-vous pour maintenir la synchronisation." }
                        },

                   });

                    canUpdate = false;
                }
            }

            //On appelle la fonction de màj du calendrier windows qui est dans Planning.xaml.cs
            if (canUpdate)
                API.UpdateWindowsCalendar(DateTime.Now.ToString("yyyy-MM-dd"), 
                                                DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"),
                                                API.GetUserFullName(ArelAPI.DataStorage.getData("user"), "Mon Planning AREL"));

            //On re-enregistre la tâche si le paramètre est présent
            if (bool.Parse(ArelAPI.DataStorage.getData("backgroundTask")))
            {

                var builder = new BackgroundTaskBuilder();
                TimeTrigger hourlyTrigger = new TimeTrigger(120, false); //On rafraîchit le planning toutes les 2 heures.

                builder.Name = "ARELSyncPlanningTask";
                builder.TaskEntryPoint = "SyncTask.ARELPlanningBackgroundTask";
                builder.SetTrigger(hourlyTrigger);
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));

                builder.Register();

            }

            _deferral.Complete();
        }
    }
}
