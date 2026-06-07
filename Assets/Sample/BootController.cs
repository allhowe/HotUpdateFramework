using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HotUpdateFramework;
using UnityEngine;

public class BootController : MonoBehaviour
{
    HotUpdateProgress _lastProgress;
    
    // Start is called before the first frame update
    void Start() {
        var config = HotUpdateConfig.LoadDefault();
        var progress = Progress.Create<HotUpdateProgress>(value => _lastProgress = value);
        HotUpdateService.Instance.RunAsync(config,progress, this.GetCancellationTokenOnDestroy());
    }
}
