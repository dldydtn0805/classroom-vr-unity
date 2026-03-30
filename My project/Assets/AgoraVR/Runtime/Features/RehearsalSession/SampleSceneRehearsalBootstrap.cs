using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgoraVR.Features.RehearsalSession
{

internal static class SampleSceneRehearsalBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSampleSceneFlowExists()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != "SampleScene")
        {
            return;
        }

        if (Object.FindAnyObjectByType<RehearsalSessionFlowController>() != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("AgoraVR Runtime Bootstrap");
        bootstrapObject.AddComponent<RehearsalSessionInstaller>();
        bootstrapObject.AddComponent<RehearsalSessionFlowController>();
    }
}
}
