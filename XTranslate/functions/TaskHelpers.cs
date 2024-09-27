using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace XTranslate.functions
{
    public class TaskHelpers
    {
        public static async Task ExecuteJob(HotkeyType job, CLICommand command = null)
        {
            await ExecuteJob(Program.DefaultTaskSettings, job, command);
        }

        public static async Task ExecuteJob(TaskSettings taskSettings)
        {
            await ExecuteJob(taskSettings, taskSettings.Job);
        }
        public static async Task ExecuteJob(TaskSettings taskSettings, HotkeyType job, CLICommand command = null)
        {
            if (job == HotkeyType.None) return;
           
            TaskSettings safeTaskSettings = TaskSettings.GetSafeTaskSettings(taskSettings);

            switch (job)
            {
                case HotkeyType.DisableHotkeys:
                    ToggleHotkeys(safeTaskSettings);
                    break;
            }
        }
        public static bool ToggleHotkeys(TaskSettings taskSettings = null)
        {
            bool disableHotkeys = !Program.Settings.DisableHotkeys;
            ToggleHotkeys(disableHotkeys, taskSettings);
            return disableHotkeys;
        }
        public static void ToggleHotkeys(bool disableHotkeys, TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            Program.Settings.DisableHotkeys = disableHotkeys;
            Program.HotkeyManager.ToggleHotkeys(disableHotkeys);
            Program.MainForm.UpdateToggleHotkeyButton();

            PlayNotificationSoundAsync(NotificationSound.ActionCompleted, taskSettings);

            if (taskSettings.GeneralSettings.ShowToastNotificationAfterTaskCompleted)
            {
                ShowNotificationTip(disableHotkeys ? Resources.TaskHelpers_ToggleHotkeys_Hotkeys_disabled_ : Resources.TaskHelpers_ToggleHotkeys_Hotkeys_enabled_);
            }
        }
    }
}
