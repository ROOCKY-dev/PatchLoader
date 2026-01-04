using System;
using System.IO;
using System.Reflection;
using PatchLoaderMod.Doorstop;
using Utils;

namespace PatchLoaderMod.DoorstopUpgrade {
    class MacOSUpgrade : IUpgradeManager {
        private Logger _logger;
        private DoorstopManager _doorstopManager;
        private ConfigManager<Config> _configManager;
        public UpgradeState State { get; private set; }

        public void UpdateState() {
            if (_doorstopManager.CheckLoaderVersionVersion()) {
                State = UpgradeState.Latest;
            } else {
                State = UpgradeState.Outdated;
            }
        }

        public void SetDoorstopManager(DoorstopManager manager) {
            _doorstopManager = manager;
        }

        public void SetLogger(Logger logger) {
            _logger = logger;
        }

        public void SetConfigManager(ConfigManager<Config> manager) {
            _configManager = manager;
        }
        
        public bool FollowToPhaseOne() {
            try {
                _logger.Info("MacOSUpgrade: Attempting to update doorstop.dylib");
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                string resourcePath = "PatchLoaderMod.Resources.macos_doorstop.dylib";
                
                using (Stream input = executingAssembly.GetManifestResourceStream(resourcePath))
                using (Stream output = File.Create("doorstop.dylib"))
                {
                    input.CopyStream(output);
                }
                
                FileExtensions.SetExecutable("doorstop.dylib");
                
                _logger.Info("MacOSUpgrade: Update successful");
                return true;
            } catch (Exception e) {
                _logger.Error("MacOSUpgrade: Update failed. " + e);
                return false;
            }
        }

        public bool FollowToPhaseTwo() {
            return false;
        }

        public bool FollowToPhaseThree() {
            return false;
        }

        public bool HandleError() {
            return false;
        }
    }
}