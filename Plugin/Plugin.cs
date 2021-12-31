using BepInEx;
using BepInEx.Configuration;
using System;
using UnityEngine;


namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(LordAshes.StatMessaging.Guid)]
    [BepInDependency(RadialUI.RadialUIPlugin.Guid)]
    public partial class CanOpenerAPIPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Can Opener API Plug-In";
        public const string Guid = "org.lordashes.plugins.canopenerapi";
        public const string Version = "1.0.0.0";

        private string data = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"/CustomData/API/";

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            // Not required but good idea to log this state for troubleshooting purpose
            UnityEngine.Debug.Log("Can Opener API Plugin: Active.");

            Utility.PostOnMainPage(this.GetType());
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            foreach (string file in System.IO.Directory.EnumerateFiles(data, "*.API"))
            {
                string result = ProcessIncoming(System.IO.File.ReadAllText(file));
                System.IO.File.Delete(file);
                System.IO.File.WriteAllText(file.ToUpper().Replace(".API", ".$$$"), result);
            }
        }
    }
}
