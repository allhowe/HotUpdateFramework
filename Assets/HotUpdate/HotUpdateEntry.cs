using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate
{
    public static class HotUpdateEntry
    {
        public static void Start()
        {
            Debug.Log("[HotUpdate] HotUpdateEntry.Start invoked.");

            Do().Forget();
        }

        private static async UniTask Do() {
            var handle = YooAsset.YooAssets.LoadAssetAsync<GameObject>("GameObject");
            await handle.ToUniTask();

            var go = handle.InstantiateSync();
            var text = go.GetComponentInChildren<Text>();
            text.text = go.name;
            
            var image = go.GetComponentInChildren<Image>();
            image.color=Color.green;
        }
    }
}
