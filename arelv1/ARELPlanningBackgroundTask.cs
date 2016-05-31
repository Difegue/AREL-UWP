using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace arelv1
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
            ArelApi API = new ArelApi();

            if (!API.isOnline()) //Si le token n'est plus valide, on en recrée un avec le login/pass qu'on a (encore une fois, idée de merde)
                API.connect_login(localSettings.Values["user"].ToString(), localSettings.Values["pass"].ToString());

            //On appelle la fonction de màj du calendrier windows qui est dans Planning.xaml.cs
            await Pages.Planning.updateWindowsCalendar(DateTime.Now.ToString("yyyy-MM-dd"), 
                                                DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"),
                                                API.getUserFullName(API.getData("user")));

            //On re-enregistre la tâche si le paramètre est présent
            if (API.getData("backgroundTask")=="true")
            {

                var builder = new BackgroundTaskBuilder();
                TimeTrigger hourlyTrigger = new TimeTrigger(120, false); //On rafraîchit le planning toutes les 2 heures.

                builder.Name = "ARELSyncPlanningTask";
                builder.TaskEntryPoint = "ARELPlanningBackgroundTask";
                builder.SetTrigger(hourlyTrigger);

                builder.Register();

            }

            _deferral.Complete();
        }
    }
}
