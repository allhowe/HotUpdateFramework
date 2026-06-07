using System;
using UnityEngine;

namespace HotUpdateFramework
{
    public sealed class HotUpdateContext
    {
        public Action OnComplete { get; set; }
        public Action<float, string> OnProgress { get; set; }
        public object UserData { get; set; }

        public void ReportProgress(float progress, string message = null)
        {
            OnProgress?.Invoke(Mathf.Clamp01(progress), message ?? string.Empty);
        }

        public void Complete()
        {
            OnComplete?.Invoke();
        }
    }
}
