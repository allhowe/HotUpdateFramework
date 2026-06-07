using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HotUpdateFramework
{
    public static class HotUpdatePlatformUtility
    {
        public static string GetPlatformName(string overrideName)
        {
            if (string.IsNullOrWhiteSpace(overrideName) == false)
                return overrideName.Trim();

#if UNITY_EDITOR
            return EditorUserBuildSettings.activeBuildTarget.ToString();
#else
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                case RuntimePlatform.OSXPlayer:
                    return "StandaloneOSX";
                case RuntimePlatform.LinuxPlayer:
                    return "StandaloneLinux64";
                case RuntimePlatform.WindowsPlayer:
                    return "StandaloneWindows64";
                default:
                    return Application.platform.ToString();
            }
#endif
        }
    }
}
