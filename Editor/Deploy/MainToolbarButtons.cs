using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Ottermation.Utils.Deploy
{
    public class MainToolbarButtons : EditorToolbarButton
    {
        public const string DeployButtonPath = "OtterTools/Deploy";
        public const string DeploySettingsButtonPath = "OtterTools/DeploySettings";
        
        [MainToolbarElement(DeployButtonPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement DeployButton()
        {
            Texture2D icon = EditorGUIUtility.IconContent("d_SignalReceiver Icon").image as Texture2D;
            MainToolbarContent content = new MainToolbarContent("Deploy", icon, "Deploy to remote machine");
            DeployConfigList configs = DeployConfigList.instance;
            return new MainToolbarButton(content, () =>
            {
                if (configs.SelectedConfigIndex == -1 || configs.SelectedConfigIndex >= configs.DeployConfigs.Count)
                {
                    Debug.LogWarning("<b>OtterUtils Deploy</b>: Could not deploy because no valid config was selected");
                }
                else
                {
                    DeployHandler.Deploy(configs.DeployConfigs[configs.SelectedConfigIndex]);
                }
            });
        }
        
        [MainToolbarElement(DeploySettingsButtonPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement SettingsButton()
        {
            DeployConfigList configs = DeployConfigList.instance;
            Texture2D icon = EditorGUIUtility.IconContent("Settings").image as Texture2D;
            string activeConfigName =
                configs.SelectedConfigIndex == -1 || configs.SelectedConfigIndex >= configs.DeployConfigs.Count
                    ? "No Active Config"
                    : configs.DeployConfigs[configs.SelectedConfigIndex].Description;
            MainToolbarContent content = new MainToolbarContent(activeConfigName, icon, $"Deploy to {activeConfigName}");
            return new MainToolbarDropdown(content, ShowDropdown);
        }
        
        private static void ShowDropdown(Rect dropdownRect)
        {
            GenericMenu menu = new GenericMenu();

            int activeConfig = DeployConfigList.instance.SelectedConfigIndex;
            for (int i = 0; i < DeployConfigList.instance.DeployConfigs.Count; i++)
            {
                DeployConfig config = DeployConfigList.instance.DeployConfigs[i];
                if (config is null)
                    continue;

                bool isActive = i == activeConfig;

                int iClone = i;
                menu.AddItem(
                    new GUIContent(config.Description),
                    isActive,
                    () =>
                    {
                        DeployConfigList.instance.SelectedConfigIndex = iClone;
                        DeployConfigList.instance.Save();
                        MainToolbar.Refresh(DeploySettingsButtonPath);
                    }
                );
            }

            menu.AddSeparator("");

            menu.AddItem(
                new GUIContent("Open Settings"),
                false,
                DeploySettingsWindow.ShowWindow
            );

            menu.DropDown(dropdownRect);
        }
    }
}