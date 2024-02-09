using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class StartButton : MonoBehaviour
    {
        public void StartGame()
        {
            if (PlayerData.instance.ftueLevel == 0)
            {
                PlayerData.instance.ftueLevel = 1;
                PlayerData.instance.Save();
            }
            
            SceneManager.LoadScene("main");
        }
    }
}
