using UnityEngine;
using System.Collections;

public enum MenuType { FreshStart, Continue }

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;
    public GameObject freshStartMenuUI;
    public GameObject continueMenuUI;
    public UIFader fader;

    void Awake() => Instance = this;

    public void ShowMenu(MenuType type)
    {
        freshStartMenuUI.SetActive(type == MenuType.FreshStart);
        continueMenuUI.SetActive(type == MenuType.Continue);
    }

    public IEnumerator FadeOutMenuAndStartGame()
    {
        yield return fader.FadeOut();
        PlayerController.Instance.EnablePlayer();
    }
}
