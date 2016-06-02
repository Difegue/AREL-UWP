using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;


namespace SyncTask
{
    public sealed class ARELPlanningBackgroundTask : IBackgroundTask
    {
        /*
         * If you run any asynchronous code in your background task, then your background task needs to use a deferral. 
         * If you don't use a deferral, then the background task process can terminate unexpectedly if the Run method 
         * completes before your asynchronous method call has completed.
         */
        
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            BackgroundTaskDeferral _deferral = taskInstance.GetDeferral();
            ArelAPI.Connector API = new ArelAPI.Connector();

            if (!API.isOnline()) //Si le token n'est plus valide, on en recrée un avec le login/pass qu'on a (encore une fois, idée de merde)
                API.connect(localSettings.Values["user"].ToString(), localSettings.Values["pass"].ToString());

            //On appelle la fonction de màj du calendrier windows qui est dans Planning.xaml.cs
            API.updateWindowsCalendar(DateTime.Now.ToString("yyyy-MM-dd"), 
                                                DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"),
                                                API.getUserFullName(API.getData("user")));

            //On re-enregistre la tâche si le paramètre est présent
            if (bool.Parse(API.getData("backgroundTask")))
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
