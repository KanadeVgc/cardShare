using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject difficultyPanel;  // 把 MainMenuPanel 拖進來

    [Header("開始按鈕")]
    public Button startGameButton;

    [Header("難度按鈕")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    public Button expertButton;

    [Header("難度圖片")]
    public Sprite easySprite;
    public Sprite mediumSprite;
    public Sprite hardSprite;
    public Sprite expertSprite;

    void Start()
    {
        if (startPanel      != null) startPanel.SetActive(true);
        if (difficultyPanel != null) difficultyPanel.SetActive(false);

        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartClicked);
        if (easyButton      != null) easyButton.onClick.AddListener(StartEasy);
        if (mediumButton    != null) mediumButton.onClick.AddListener(StartMedium);
        if (hardButton      != null) hardButton.onClick.AddListener(StartHard);
        if (expertButton    != null) expertButton.onClick.AddListener(StartExpert);

        SetButtonSprite(easyButton,   easySprite);
        SetButtonSprite(mediumButton, mediumSprite);
        SetButtonSprite(hardButton,   hardSprite);
        SetButtonSprite(expertButton, expertSprite);
    }

    void OnStartClicked()
    {
        if (startPanel      != null) startPanel.SetActive(false);
        if (difficultyPanel != null) difficultyPanel.SetActive(true);
    }

    void StartEasy()   { GameSettings.SelectedDifficulty = GameManager.Difficulty.Easy;   GoToGame(); }
    void StartMedium() { GameSettings.SelectedDifficulty = GameManager.Difficulty.Medium; GoToGame(); }
    void StartHard()   { GameSettings.SelectedDifficulty = GameManager.Difficulty.Hard;   GoToGame(); }
    void StartExpert() { GameSettings.SelectedDifficulty = GameManager.Difficulty.Expert; GoToGame(); }

    void GoToGame() => SceneManager.LoadScene("SampleScene");

    void SetButtonSprite(Button btn, Sprite sprite)
    {
        if (btn == null || sprite == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
        }

        // 隱藏原本按鈕內的文字
        UnityEngine.UI.Text txt = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (txt != null) txt.gameObject.SetActive(false);

        TMPro.TextMeshProUGUI tmpTxt = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmpTxt != null) tmpTxt.gameObject.SetActive(false);
    }
}