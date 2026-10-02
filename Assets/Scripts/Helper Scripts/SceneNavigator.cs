using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One guarded asynchronous scene transition, including direct-scene play in the editor.</summary>
public sealed class SceneNavigator : MonoBehaviour
{
    public static bool IsLoading { get; private set; }
    private static SceneNavigator active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { IsLoading = false; active = null; }

    public static bool CanLoad(string scene)
    {
        if (IsLoading) return false;
        if (Application.CanStreamedLevelBeLoaded(scene)) return true;
        Debug.LogError("Add " + scene + " to Build Settings before starting a run.");
        if (RunnerUI.instance != null) RunnerUI.instance.ShowToast("This track is unavailable. Check Build Settings.");
        return false;
    }

    public static bool Load(string scene)
    {
        if (!CanLoad(scene)) return false;
        IsLoading = true;
        Time.timeScale = 1f;
        if (GameManager.instance != null) GameManager.instance.FlushSave();
        RunnerUI oldUI = RunnerUI.instance;
        if (oldUI != null) oldUI.ShowLoading();
        GameObject host = new GameObject("Scene transition");
        DontDestroyOnLoad(host);
        active = host.AddComponent<SceneNavigator>();
        active.StartCoroutine(active.Transition(scene, oldUI));
        return true;
    }

    private IEnumerator Transition(string scene, RunnerUI oldUI)
    {
        yield return null; // Render the loading state before importing the new scene.
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single); }
        catch (Exception ex) { Debug.LogError("Scene load failed: " + ex.Message); }
        if (operation == null)
        {
            IsLoading = false;
            if (oldUI != null) oldUI.SceneLoadFailed();
            Destroy(gameObject);
            yield break;
        }
        while (!operation.isDone) yield return null;
        IsLoading = false;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // A retiring host must not clear a newer transition started in the same frame.
        if (active == this) { active = null; IsLoading = false; }
    }
}
