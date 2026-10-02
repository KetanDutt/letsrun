using UnityEngine;

/// <summary>Android Back/Escape closes menu dialogs before exiting; gameplay owns its pause key.</summary>
public sealed class ExitManager : MonoBehaviour
{
    private MainMenuController menu;

    private void Start() { menu = FindObjectOfType<MainMenuController>(); }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || SceneNavigator.IsLoading || GameplayController.instance != null) return;
        if (menu != null && menu.HandleBack()) return;
        GameManager.EnsureInstance().FlushSave();
        Application.Quit();
    }
}
