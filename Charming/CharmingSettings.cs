using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;

namespace CharmingMod
{
    public class CharmingSettingsVars
    {
        //change when the global settings are updated to force a recreation of the global settings
        public const string ModVersion = "0.0.1";
        //change when the global settings are updated to force a recreation of the global settings
        public const string GlobalSettingsVersion = "0.0.1";
    }

    //Global (non-player specific) settings
    public class CharmingModSettings : IModSettings
    {
        public void Reset()
        {
            BoolValues.Clear();
            StringValues.Clear();
            IntValues.Clear();
            FloatValues.Clear();
        }

        public string SettingsVersion {
            get => GetString( "0.0.0" );
            set {
                SetString( value );
            }
        }       
    }

    //Player specific settings
    public class CharmingModSaveSettings : IModSettings
    {
    }
}
