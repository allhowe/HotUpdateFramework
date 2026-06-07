using System;
using System.IO;
using HotUpdateFramework;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace HotUpdateFramework.Editor
{
    public static class HotUpdateBuildMenu
    {
        private const string MenuRoot = "Hot Update/";
        private const string ConfigAssetPath = "Assets/Resources/HotUpdateConfig.asset";

        [MenuItem(MenuRoot + "Create Default Config", priority = 1)]
        public static HotUpdateConfig CreateDefaultConfigAsset()
        {
            HotUpdateConfig existing = AssetDatabase.LoadAssetAtPath<HotUpdateConfig>(ConfigAssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                return existing;
            }

            EnsureDirectory(Path.GetDirectoryName(ConfigAssetPath));
            var config = ScriptableObject.CreateInstance<HotUpdateConfig>();
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = config;
            Debug.Log($"[HotUpdate] Created config: {ConfigAssetPath}");
            return config;
        }

        [MenuItem(MenuRoot + "Generate HybridCLR/Compile HotUpdate DLLs", priority = 20)]
        public static void CompileHotUpdateDlls()
        {
            CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.development);
        }

        [MenuItem(MenuRoot + "Generate HybridCLR/Generate All", priority = 21)]
        public static void GenerateHybridClrAll()
        {
            PrebuildCommand.GenerateAll();
        }

        [MenuItem(MenuRoot + "Prepare YooAsset DLL Assets", priority = 40)]
        public static void PrepareYooAssetRawFiles()
        {
            HotUpdateConfig config = GetOrCreateConfig();
            int copiedCount = 0;

            foreach (string location in config.HotUpdateAssemblyAssetLocations)
            {
                if (string.IsNullOrWhiteSpace(location))
                    continue;

                string dllFileName = GetDllFileNameFromAssetLocation(location);
                string sourcePath = GetFullPath(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget), dllFileName);
                if (CopyIfExists(sourcePath, location))
                    copiedCount++;
            }

            foreach (string location in config.AotMetadataAssetLocations)
            {
                if (string.IsNullOrWhiteSpace(location))
                    continue;

                string dllFileName = GetDllFileNameFromAssetLocation(location);
                string sourcePath = GetFullPath(SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget), dllFileName);
                if (CopyIfExists(sourcePath, location))
                    copiedCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[HotUpdate] Prepared YooAsset DLL assets. Copied: {copiedCount}");
        }

        [MenuItem(MenuRoot + "Build YooAsset Package", priority = 60)]
        public static void BuildYooAssetPackage()
        {
            HotUpdateConfig config = GetOrCreateConfig();

            string packageVersion = string.IsNullOrWhiteSpace(config.PackageVersionOverride)
                ? DateTime.Now.ToString("yyyyMMddHHmmss")
                : config.PackageVersionOverride;
            EBuildinFileCopyOption buildinFileCopyOption = config.UseBuildinFileSystemInHostMode
                ? EBuildinFileCopyOption.ClearAndCopyAll
                : EBuildinFileCopyOption.None;

            ScriptableBuildParameters buildParameters = new ScriptableBuildParameters
            {
                BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot(),
                BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString(),
                BuildBundleType = (int)EBuildBundleType.AssetBundle,
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                PackageName = config.PackageName,
                PackageVersion = packageVersion,
                PackageNote = "Hot update package",
                EnableSharePackRule = true,
                VerifyBuildingResult = true,
                FileNameStyle = EFileNameStyle.HashName,
                BuildinFileCopyOption = buildinFileCopyOption,
                BuildinFileCopyParams = string.Empty,
                CompressOption = ECompressOption.LZ4,
                ClearBuildCacheFiles = false,
                UseAssetDependencyDB = true,
                BuiltinShadersBundleName = GetBuiltinShaderBundleName(config.PackageName)
            };

            Debug.Log($"[HotUpdate] Buildin file copy option: {buildinFileCopyOption}");

            var pipeline = new ScriptableBuildPipeline();
            BuildResult buildResult = pipeline.Run(buildParameters, true);
            if (buildResult.Success)
            {
                EditorUtility.RevealInFinder(buildResult.OutputPackageDirectory);
                Debug.Log($"[HotUpdate] YooAsset package built: {buildResult.OutputPackageDirectory}");
            }
            else
            {
                throw new Exception($"YooAsset build failed: {buildResult.FailedTask}, {buildResult.ErrorInfo}");
            }
        }

        private static string GetBuiltinShaderBundleName(string packageName)
        {
            bool uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
            PackRuleResult packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            return packRuleResult.GetBundleName(packageName, uniqueBundleName);
        }

        private static HotUpdateConfig GetOrCreateConfig()
        {
            return HotUpdateConfig.LoadDefault() ?? CreateDefaultConfigAsset();
        }

        private static bool CopyIfExists(string sourcePath, string destinationAssetPath)
        {
            if (File.Exists(sourcePath) == false)
            {
                Debug.LogWarning($"[HotUpdate] Missing source file: {sourcePath}");
                return false;
            }

            string destinationFullPath = GetProjectFullPath(destinationAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFullPath));
            File.Copy(sourcePath, destinationFullPath, true);
            Debug.Log($"[HotUpdate] Copy {sourcePath} -> {destinationAssetPath}");
            return true;
        }

        private static string GetDllFileNameFromAssetLocation(string assetLocation)
        {
            string fileName = Path.GetFileName(assetLocation);
            if (fileName.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase))
                fileName = fileName.Substring(0, fileName.Length - ".bytes".Length);
            return fileName;
        }

        private static string GetFullPath(string relativeDirectory, string fileName)
        {
            return Path.GetFullPath(Path.Combine(SettingsUtil.ProjectDir, relativeDirectory.Replace('/', Path.DirectorySeparatorChar), fileName));
        }

        private static string GetProjectFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(SettingsUtil.ProjectDir, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void EnsureDirectory(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            string[] parts = assetPath.Replace("\\", "/").Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (AssetDatabase.IsValidFolder(next) == false)
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
