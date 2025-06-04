using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public SaveData currentSave;
    public bool hasSave = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        TryLoadGame();
    }

    void TryLoadGame()
    {
        currentSave = SaveSystem.LoadLatestSave();

        if (currentSave != null)
        {
            hasSave = true;
            MenuManager.Instance.ShowMenu(MenuType.Continue);
        }
        else
        {
            hasSave = false;
            MenuManager.Instance.ShowMenu(MenuType.FreshStart);
        }
    }

    public void StartNewGame()
    {
        currentSave = new SaveData(); // reset data
        hasSave = true;
        StartCoroutine(MenuManager.Instance.FadeOutMenuAndStartGame());
    }

    public void ContinueGame()
    {
        StartCoroutine(MenuManager.Instance.FadeOutMenuAndStartGame());
    }
}
