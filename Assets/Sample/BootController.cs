using Cysharp.Threading.Tasks;
using HotUpdateFramework;
using UnityEngine;
using UnityEngine.UI;

public class BootController : MonoBehaviour
{
    [SerializeField] private GameObject loadingView;
    [SerializeField] private Slider slider;

    private HotUpdateProgress _lastProgress;

    private void Start()
    {
        var config = HotUpdateConfig.LoadDefault();
        var progress = Progress.Create<HotUpdateProgress>(OnHotUpdateProgress);
        var context = new HotUpdateContext
        {
            OnComplete = OnHotUpdateComplete,
            OnProgress = OnHotUpdateContextProgress,
            UserData = this
        };

        HotUpdateService.Instance.RunAsync(config, progress, this.GetCancellationTokenOnDestroy(), context).Forget();
    }

    private void OnHotUpdateProgress(HotUpdateProgress value)
    {
        _lastProgress = value;
        if (slider != null) {
            slider.value = value.Progress;
        }
    }

    private void OnHotUpdateContextProgress(float progress, string message)
    {
        Debug.Log($"[Boot] Hot update context progress {progress:P0} {message}");
    }

    private void OnHotUpdateComplete()
    {
        if (loadingView != null)
            loadingView.SetActive(false);

        Debug.Log("[Boot] Hot update entry completed.");
    }
}
