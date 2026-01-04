using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PatchLoaderMod.DoorstopUpgrade;
using Utils;

namespace PatchLoaderMod.Doorstop {
    public class MacOSDoorstopManager: DoorstopManager {
        public override string LoaderMD5 => "878ce9acec5a81571c3750f243efab53";
        public override bool RequiresRestart { get; protected set; } = false;
        public override bool PlatformSupported => true;

        public override string InstallMessage { get; } = "The game will be closed.\n\n" +
                                                         "\tIMPORTANT!\n\n" +
                                                         "If you use Paradox game launcher:\n" +
                                                         "  1. Open main game directory (Cities.app) navigate to " +
                                                         "     /Contents/Launcher directory and search for launcher-settings.json\n" +
                                                         "  2. Make backup of that file (e.g. create copy with different name)\n" +
                                                         "  3. Open launcher-settings.json using any text editor and change" +
                                                         " 'exePath' value to '../../../Cities_Loader.sh' instead of" +
                                                         " original '../MacOS/Cities'\n" +
                                                         "  4. Save file and run game normally\n\n" +
                                                         "---------------------------------------------------------------------\n" +
                                                         "Or if don't use Paradox game launcher:\n" +
                                                         "  1. Add './Cities_Loader.sh %command%' (without quotes) to the game Steam Set Launch Options\n" +
                                                         "    in the Steam Client\n" +
                                                         "  2. Run game normally\n" +
                                                         "---------------------------------------------------------------------\n" +
                                                         "If game won't launch remove commandline parameter or restore backup launcher-settings.json\n" +
                                                         "and contact the mod author for more solutions";
        public override string UninstallMessage { get; } = "The game will be closed.\n\n";

        public override bool CanEnable { get; } = true;

        private UnixConfigProperties _configProperties = new UnixConfigProperties(
            "#!/bin/sh\n" +
                    "doorstop_libname=\"doorstop.dylib\"\n" +
                    "doorstop_dir=$PWD\n" +
                    "export DYLD_LIBRARY_PATH=${doorstop_dir}:${DYLD_LIBRARY_PATH};",
            "export DYLD_INSERT_LIBRARIES",
            "export DOORSTOP_ENABLED",
            "export DOORSTOP_TARGET_ASSEMBLY",
            "./Cities.app/Contents/MacOS/Cities $@"
        );

        public MacOSDoorstopManager(string expectedTargetAssemblyPath, Logger logger) : base(expectedTargetAssemblyPath, logger) {
            logger.Info("Instantiating MacOSDoorstopManager");
            _loaderFileName = "doorstop.dylib";
            _configFileName = "Cities_Loader.sh";
            UpgradeManager = new MacOSUpgrade();
        }

        protected override string BuildConfig() {
            return new StringBuilder()
                .AppendLine(_configProperties.Header)
                .Append(_configProperties.PreloadKey).AppendLine("=$doorstop_libname;")
                .Append(_configProperties.EnabledStateKey).Append("=").Append(_configValues.Enabled.ToString().ToUpper()).AppendLine(";")
                .Append(_configProperties.TargetAssemblyKey).Append("=\"").Append(_configValues.TargetAssembly).AppendLine("\";")
                .AppendLine("echo \"[Cities_Loader] Launching with Doorstop...\"")
                .AppendLine("echo \"[Cities_Loader] DOORSTOP_ENABLED=$DOORSTOP_ENABLED\"")
                .AppendLine("echo \"[Cities_Loader] DOORSTOP_TARGET_ASSEMBLY=$DOORSTOP_TARGET_ASSEMBLY\"")
                .AppendLine("echo \"[Cities_Loader] DYLD_INSERT_LIBRARIES=$DYLD_INSERT_LIBRARIES\"")
                .AppendLine("echo \"[Cities_Loader] DYLD_LIBRARY_PATH=$DYLD_LIBRARY_PATH\"")
                .Append(_configProperties.GameExePath)
                .ToString();
        }

        private string ExtractInsertLibEnvVariable() {
            string env = Environment.GetEnvironmentVariable("DYLD_INSERT_LIBRARIES") ?? "";
            if (env.Contains("$doorstop_libname")) {
                string[] values = env.Split(':').Where(v => !v.StartsWith("$doorstop_libname")).ToArray();
                env = string.Join(":", values);
            }

            return env;
        }

        protected override ConfigValues InternalLoadConfig(string[] lines) {
            string[] preloadValues = lines[4].Split('=');
            string preloadValue = preloadValues[1];
            
            string[] stateKeyValue = lines[5].Split('=');
            var enabled = bool.Parse(stateKeyValue[1].ToLower().Trim(';'));

            string[] targetPathKeyValue = lines[6].Split('=');
            var targetAssembly = targetPathKeyValue[1].Trim('"', ';');

            _logger.Info($"Loader config parsing complete. Status: [{(enabled ? "enabled" : "disabled")}] Loader assembly path [{targetAssembly}] PreloadValue [{preloadValue}]");

            return new ConfigValues(enabled, targetAssembly, false/*todo !preloadValue.Contains("Application Support")*/);
        }

        protected override void InstallLoader() {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string resourcePath = $"PatchLoaderMod.Resources.macos_doorstop.dylib";
            _logger._Debug("Resource path: " + resourcePath);

            using (Stream input = executingAssembly.GetManifestResourceStream(resourcePath))
            using (Stream output = File.Create("doorstop.dylib"))
            {
                _logger._Debug("Copying stream.");
                input.CopyStream(output);
            }
            
            FileExtensions.SetExecutable("doorstop.dylib");
        }
        
        internal void GrantExecuteAccessForConfig() {
            _logger.Info($"Granting execute permission to {_configFileName}");
            FileExtensions.SetExecutable(_configFileName);
        }
        
        protected override bool IsLatestLoaderVersion() {
            if (!IsLoaderInstalled())
            {
                return false;
            }

            try
            {
                return FileExtensions.CalculateFileMd5Hash(LoaderFileName) == LoaderMD5;
            }
            catch (Exception e)
            {
                _logger.Error("Could not calculate hash for file " + LoaderFileName + ". Exception: " + e);
                return false;
            }
        }
    }
}