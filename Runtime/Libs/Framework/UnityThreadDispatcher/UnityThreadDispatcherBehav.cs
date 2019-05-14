using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityThreadDispatcherBehav : MonoBehaviour
{
	void Update ()
    {
		if (Capstones.UnityEngineEx.UnityThreadDispatcher._RunningObj != gameObject)
        {
            Destroy(gameObject);
            return;
        }

        Capstones.UnityEngineEx.UnityThreadDispatcher.HandleEvents();
    }
}
