using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartFolder
{
    [Serializable]
    public class FolderCustomization
    {
        public string icon;
        public string color;
    }

    [Serializable]
    public class SmartFolderSettings
    {
        public Dictionary<string, FolderCustomization> customIcons = new Dictionary<string, FolderCustomization>();
        public List<string> activeModules = new List<string>() { "stacking", "twoLine" };

        public bool IsModuleActive(string moduleName)
        {
            return activeModules.Contains(moduleName);
        }

        public void SetModuleActive(string moduleName, bool active)
        {
            if (active && !activeModules.Contains(moduleName))
            {
                activeModules.Add(moduleName);
            }
            else if (!active && activeModules.Contains(moduleName))
            {
                activeModules.Remove(moduleName);
            }
        }
    }
}
