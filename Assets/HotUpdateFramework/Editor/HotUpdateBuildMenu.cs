using UnityEditor;

namespace HotUpdateFramework.Editor
{
    public static class HotUpdateBuildMenu
    {
        public const string MenuRoot = "Hot Update/";

        [MenuItem(MenuRoot + "Create Config", priority = 1)]
        public static HotUpdateConfig CreateDefaultConfigAsset()
        {
            return HotUpdateEditorUtility.CreateDefaultConfigAsset();
        }

        [MenuItem(MenuRoot + "Prepare All Process", priority = 20)]
        public static void PrepareAllProcess()
        {
            HotUpdateBuildPipeline.PrepareAllProcess();
        }

        [MenuItem(MenuRoot + "Prepare HotUpdate Process", priority = 21)]
        public static void PrepareHotUpdateDlls()
        {
            HotUpdateBuildPipeline.PrepareHotUpdateProcess();
        }

        [MenuItem(MenuRoot + "Build YooAsset Package", priority = 60)]
        public static void BuildYooAssetPackage()
        {
            HotUpdateBuildPipeline.BuildPackage();
        }

        [MenuItem(MenuRoot + "Clear/Build Cache", priority = 80)]
        public static void ClearYooAssetBuildCache()
        {
            HotUpdateBuildPipeline.ClearBuildCache();
        }

        [MenuItem(MenuRoot + "Clear/Editor Runtime Cache", priority = 81)]
        public static void ClearYooAssetEditorRuntimeCache()
        {
            HotUpdateBuildPipeline.ClearEditorRuntimeCache();
        }
    }
}
