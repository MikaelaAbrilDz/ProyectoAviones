using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void OpenOnline()
    {
        SceneManager.LoadScene("OnlineMultiScene");
    }
    public void OpenLocal()
    {
        SceneManager.LoadScene("LocalMultiScene");
    }
}
