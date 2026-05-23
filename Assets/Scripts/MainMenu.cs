using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
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
    }
}