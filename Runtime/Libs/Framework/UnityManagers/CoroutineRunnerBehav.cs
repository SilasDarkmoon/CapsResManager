using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineRunnerBehav : MonoBehaviour
{
    private void Update()
    {
        Capstones.UnityEngineEx.CoroutineRunner.DisposeDeadCoroutines();
    }

    private void OnDestroy()
    {
        Capstones.UnityEngineEx.CoroutineRunner.DisposeAllCoroutinesOnDestroyRunner(this);
    }
}
