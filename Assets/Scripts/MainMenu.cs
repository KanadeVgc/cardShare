using UnityEngine;
<<<<<<< Updated upstream
=======
using UnityEngine.UI;
>>>>>>> Stashed changes
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
<<<<<<< Updated upstream
    public void StartEasy()
    {
        GameSettings.SelectedDifficulty =
            GameManager.Difficulty.Easy;

        SceneManager.LoadScene("SampleScene");
    }

    public void StartMedium()
    {
        GameSettings.SelectedDifficulty =
            GameManager.Difficulty.Medium;

        SceneManager.LoadScene("SampleScene");
    }

    public void StartHard()
    {
        GameSettings.SelectedDifficulty =
            GameManager.Difficulty.Hard;

        SceneManager.LoadScene("SampleScene");
    }

    public void StartExpert()
    {
        GameSettings.SelectedDifficulty =
            GameManager.Difficulty.Expert;

        SceneManager.LoadScene("SampleScene");
=======
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject difficultyPanel;  // 把 MainMenuPanel 拖進來
    public GameObject instructionPanel; // 遊戲說明面板

    [Header("開始與說明按鈕")]
    public Button startGameButton;
    public Button openInstructionButton;  // 打開說明按鈕
    public Button closeInstructionButton; // 關閉說明按鈕

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
        if (instructionPanel != null) instructionPanel.SetActive(false);

        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartClicked);
        if (openInstructionButton != null) openInstructionButton.onClick.AddListener(OpenInstruction);
        if (closeInstructionButton != null) closeInstructionButton.onClick.AddListener(CloseInstruction);
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

    void OpenInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickSFX();
    }

    void CloseInstruction()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickSFX();
    }

    void StartEasy()   { GameSettings.SelectedDifficulty = GameManager.Difficulty.Easy;   GoToGame(); }
    void StartMedium() { GameSettings.SelectedDifficulty = GameManager.Difficulty.Medium; GoToGame(); }
    void StartHard()   { GameSettings.SelectedDifficulty = GameManager.Difficulty.Hard;   GoToGame(); }
    void StartExpert() { GameSettings.SelectedDifficulty = GameManager.Difficulty.Expert; GoToGame(); }

    void GoToGame() => SceneManager.LoadScene("SampleScene");

    void SetButtonSprite(Button btn, Sprite sprite)
    {
        if (btn == null || sprite == null) return;
        btn.GetComponent<Image>().sprite = sprite;
>>>>>>> Stashed changes
    }
}