using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIStartGame : MonoBehaviour
{
     public void LoadMainGame()
    {
        SceneManager.LoadScene("MainScene"); // exact scene name
    }
}
