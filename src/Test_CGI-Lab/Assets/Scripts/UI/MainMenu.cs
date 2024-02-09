using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenu : MonoBehaviour
    {
        public void LoadScene(string name)
        {
            SceneManager.LoadScene(name);
        }

    }
}
