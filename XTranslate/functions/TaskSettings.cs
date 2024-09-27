using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XTranslate.functions
{
    public class TaskSettings
    {
        [JsonIgnore]
        public TaskSettings TaskSettingsReference { get; private set; }

        [JsonIgnore]
        public bool IsSafeTaskSettings => TaskSettingsReference != null;

        public string Description = "";

        public HotkeyType Job = HotkeyType.None;

        public static TaskSettings GetDefaultTaskSettings()
        {
            TaskSettings taskSettings = new TaskSettings();
            taskSettings.SetDefaultSettings();
            taskSettings.TaskSettingsReference = Program.DefaultTaskSettings;
            return taskSettings;
        }

        public static TaskSettings GetSafeTaskSettings(TaskSettings taskSettings)
        {
            TaskSettings safeTaskSettings;

            if (taskSettings.IsUsingDefaultSettings && Program.DefaultTaskSettings != null)
            {
                safeTaskSettings = Program.DefaultTaskSettings.Copy();
                safeTaskSettings.Description = taskSettings.Description;
                safeTaskSettings.Job = taskSettings.Job;
            }
            else
            {
                safeTaskSettings = taskSettings.Copy();
                safeTaskSettings.SetDefaultSettings();
            }

            safeTaskSettings.TaskSettingsReference = taskSettings;
            return safeTaskSettings;
        }
        public void SetDefaultSettings()
        {
            if (Program.DefaultTaskSettings != null)
            {
                TaskSettings defaultTaskSettings = Program.DefaultTaskSettings.Copy();

                if (UseDefaultAfterCaptureJob)
                {
                    AfterCaptureJob = defaultTaskSettings.AfterCaptureJob;
                }

                if (UseDefaultAfterUploadJob)
                {
                    AfterUploadJob = defaultTaskSettings.AfterUploadJob;
                }

                if (UseDefaultDestinations)
                {
                    ImageDestination = defaultTaskSettings.ImageDestination;
                    ImageFileDestination = defaultTaskSettings.ImageFileDestination;
                    TextDestination = defaultTaskSettings.TextDestination;
                    TextFileDestination = defaultTaskSettings.TextFileDestination;
                    FileDestination = defaultTaskSettings.FileDestination;
                    URLShortenerDestination = defaultTaskSettings.URLShortenerDestination;
                    URLSharingServiceDestination = defaultTaskSettings.URLSharingServiceDestination;
                }

                if (UseDefaultGeneralSettings)
                {
                    GeneralSettings = defaultTaskSettings.GeneralSettings;
                }

                if (UseDefaultImageSettings)
                {
                    ImageSettings = defaultTaskSettings.ImageSettings;
                }

                if (UseDefaultCaptureSettings)
                {
                    CaptureSettings = defaultTaskSettings.CaptureSettings;
                }

                if (UseDefaultUploadSettings)
                {
                    UploadSettings = defaultTaskSettings.UploadSettings;
                }

                if (UseDefaultActions)
                {
                    ExternalPrograms = defaultTaskSettings.ExternalPrograms;
                }

                if (UseDefaultToolsSettings)
                {
                    ToolsSettings = defaultTaskSettings.ToolsSettings;
                }

                if (UseDefaultAdvancedSettings)
                {
                    AdvancedSettings = defaultTaskSettings.AdvancedSettings;
                }
            }
        }
    }
}
