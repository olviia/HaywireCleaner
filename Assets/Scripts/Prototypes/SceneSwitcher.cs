using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prototypes
{
    public class SceneSwitcher : MonoBehaviour
    {
        [SerializeField] private string scene;

        public void GoToScene()
        {
            SceneManager.LoadScene(scene);
        }
    }
}
