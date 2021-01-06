namespace Scellecs.Unity {
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using dnlib.DotNet;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public class XCodeRunFix {
        private string xcodePath;

        [MenuItem("Tools/XCodeRunFix/Fix it")]
        public static void Go() {
            const string libraryName = "UnityEditor.iOS.Extensions.Common.dll";

            var separator   = Path.DirectorySeparatorChar;
            var rootPath    = Path.GetDirectoryName(EditorApplication.applicationPath);
            var libraryPath = rootPath + separator + "PlaybackEngines" + separator + "iOSSupport" + separator + libraryName;
            var backupPath  = Application.dataPath + separator + libraryName + ".backup";

            File.Copy(libraryPath, backupPath, true);
            Debug.Log($"Backup DLL from {libraryPath} to {backupPath}.\nFor restore changes just copy it back or reinstall iOS Support Module.");

            var currentMod   = ModuleDefMD.Load(typeof(XCodeRunFix).Module);
            var currentType  = currentMod.Types.First(t => t.Name == nameof(XCodeRunFix));
            var injectMethod = currentType.Methods.First(m => m.Name == nameof(RunProject));

            var module  = ModuleDefMD.Load(libraryPath);
            var typeDef = module.Types.First(t => t.Name == "XcodeController");

            var xcodePathDef    = currentType.Fields.First(f => f.Name == nameof(xcodePath));
            var xcodePathNewDef = typeDef.Fields.First(f => f.Name == nameof(xcodePath));
            foreach (var instruction in injectMethod.Body.Instructions) {
                if (instruction.Operand == xcodePathDef) {
                    instruction.Operand = xcodePathNewDef;
                }
            }

            typeDef.Methods.First(m => m.Name == nameof(RunProject)).Body = injectMethod.Body;

            module.Write(libraryPath);
            
            Debug.Log($"Now you can uninstall this package. Enjoy.");
            CompilationPipeline.RequestScriptCompilation();
        }

        public void RunProject(string projectPath) {
            var args = $"-e \'tell application \"{this.xcodePath}\" \' " +
                       "-e \'activate\' " +
                       "-e \'set targetProject to active workspace document\' " +
                       "-e \'run targetProject\' " +
                       "-e \'end tell\'";

            Process.Start(new ProcessStartInfo("osascript", args) {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            });
        }
    }
}