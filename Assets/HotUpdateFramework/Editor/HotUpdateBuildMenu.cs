using HybridCLR.Editor.Commands;
using UnityEditor;

namespace HotUpdateFramework.Editor
{
    public static class HotUpdateBuildMenu
    {
        public const string MenuRoot = "Hot Update/";

        [MenuItem(MenuRoot + "Create Config", priority = 1)]
        public static HotUpdateConfig CreateDefaultConfigAsset()
        {
            return HotUpdateHelper.CreateDefaultConfigAsset();
        }

        [MenuItem(MenuRoot + "Prepare All Process", priority = 20)]
        public static void PrepareAllProcess()
        {
            PrebuildCommand.GenerateAll();
            HotUpdateHelper.SyncAotMetadataList();
            HotUpdateHelper.CopyAotMetadataAndHotUpdateDlls();
        }

        [MenuItem(MenuRoot + "Prepare HotUpdate Process", priority = 21)]
        public static void PrepareHotUpdateDlls()
        {
            CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.development);
            HotUpdateHelper.CopyAotMetadataAndHotUpdateDlls();
        }

        [MenuItem(MenuRoot + "Build YooAsset Package", priority = 60)]
        public static void BuildYooAssetPackage()
        {
            HotUpdateHelper.BuildYooAssetPackage();
        }

        [MenuItem(MenuRoot + "Clear/Build Cache", priority = 80)]
        public static void ClearYooAssetBuildCache()
        {
            HotUpdateHelper.ClearYooAssetBuildCache();
        }

        [MenuItem(MenuRoot + "Clear/Editor Runtime Cache", priority = 81)]
        public static void ClearYooAssetEditorRuntimeCache()
        {
            HotUpdateHelper.ClearYooAssetEditorRuntimeCache();
        }
    }
}