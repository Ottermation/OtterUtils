using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ottermation.Utils.Deploy
{
    [FilePath("UserSettings/OtterUtils/DeployTargetList.asset", FilePathAttribute.Location.ProjectFolder)]
    public class DeployConfigList : ScriptableSingleton<DeployConfigList>
    {
        public List<DeployConfig> DeployConfigs = new();
        public int SelectedConfigIndex = -1;

        public void Save()
        {
            Save(true);
        }
    }
}