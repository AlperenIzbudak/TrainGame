using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public Button boardSceneChangeButton;
    
    private void Start()
    {
        boardSceneChangeButton.onClick.AddListener(() => ChangeScene("Board"));
    }
    
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
