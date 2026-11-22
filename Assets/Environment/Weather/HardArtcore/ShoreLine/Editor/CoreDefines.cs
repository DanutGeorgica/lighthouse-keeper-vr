using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace HardArtcore.ShoreLine.Editor
{
    /// <summary>
    /// Keeps the project scripting defines HAW_SHORELINE and HAW_SHORELINE_{major}_{minor} (cumulative)
    /// in sync with the installed core. Add-on assemblies use them as defineConstraints, so with the core missing
    /// or too old Unity skips the add-on cleanly instead of failing with compile errors. Part of the add-on contract.
    /// (Asmdef versionDefines can't be used: they only see registered packages, and the core ships in Assets/.)
    /// </summary>
    [InitializeOnLoad]
    public static class CoreDefines
    {
        public const string Prefix = "HAW_SHORELINE";
        public const string CoreRoot = "Assets/HardArtcore/ShoreLine";

        static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Standalone, NamedBuildTarget.Android, NamedBuildTarget.iOS,
            NamedBuildTarget.WebGL, NamedBuildTarget.Server,
        };

        static CoreDefines()
        {
            // Defer: changing defines during assembly load would trigger a recompile mid-import.
            EditorApplication.delayCall += () => Apply(ShoreLineInfo.CoreVersion);
        }

        public static string[] RequiredDefines(Version core)
        {
            var list = new List<string> { Prefix };
            for (int minor = 0; minor <= core.Minor; minor++)
                list.Add($"{Prefix}_{core.Major}_{minor}");
            return list.ToArray();
        }

        /// <summary>Appends the missing required defines; keeps every existing define and its order.</summary>
        public static string MergeDefines(string existing, string[] required)
        {
            var defines = Split(existing);
            foreach (var d in required)
                if (!defines.Contains(d)) defines.Add(d);
            return string.Join(";", defines);
        }

        /// <summary>Removes HAW_SHORELINE and HAW_SHORELINE_x_y only.</summary>
        public static string RemoveCoreDefines(string existing) =>
            string.Join(";", Split(existing).Where(d => !IsCoreDefine(d)));

        /// <summary>The define string for this core version: stale core defines (e.g. from a newer core) are dropped.</summary>
        public static string Updated(string existing, Version core) =>
            MergeDefines(RemoveCoreDefines(existing), RequiredDefines(core));

        /// <summary>True if deleting <paramref name="path"/> deletes the core folder (the folder itself or a parent).</summary>
        public static bool DeletesCoreRoot(string path)
        {
            path = path.TrimEnd('/');
            return path == CoreRoot || CoreRoot.StartsWith(path + "/", StringComparison.Ordinal);
        }

        public static void Apply(Version core)
        {
            foreach (var target in Targets)
            {
                string current = PlayerSettings.GetScriptingDefineSymbols(target);
                string updated = Updated(current, core);
                if (updated != current) PlayerSettings.SetScriptingDefineSymbols(target, updated); // only on change: no recompile loop
            }
        }

        public static void RemoveAll()
        {
            foreach (var target in Targets)
            {
                string current = PlayerSettings.GetScriptingDefineSymbols(target);
                string updated = RemoveCoreDefines(current);
                if (updated != current) PlayerSettings.SetScriptingDefineSymbols(target, updated);
            }
        }

        static bool IsCoreDefine(string d) => d == Prefix || d.StartsWith(Prefix + "_", StringComparison.Ordinal);

        static List<string> Split(string defines) =>
            (defines ?? "").Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

        /// <summary>Removes the defines when the core folder is deleted from inside Unity, so add-ons switch off cleanly.</summary>
        class CoreDeletionWatcher : AssetModificationProcessor
        {
            static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
            {
                if (DeletesCoreRoot(path)) RemoveAll();
                return AssetDeleteResult.DidNotDelete;
            }
        }
    }
}
