using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Level {
    public class LevelLoader : MonoBehaviour
    {
        public Animator transition;
        public float transitionTime = 1f;
        private string certainScened;
        public StartScreenManager ss;

        /*
    private void Awake()
    {
        if (levelLoader == null)
        {
            levelLoader = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        { 
            Destroy(gameObject);
        }
    }
    */

        public void LoadNextLevel()
        {
            StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex + 1));
        }

        public void LoadCertainScene(string certainScene)
        {
            if (ss!=null)
            {
                if (ss.cantPress)
                {
                    return;   
                }
                ss.cantPress = true;
            }
            
            Time.timeScale = 1f;
            StartCoroutine(LoadCertainScene());
            certainScened = certainScene;
        }

        public void Restart()
        {
            StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex));
        }

        private IEnumerator LoadLevel(int levelIndex)
        {
            Time.timeScale = 1;
            transition.SetTrigger("Start");
            yield return new WaitForSeconds(transitionTime);
            SceneManager.LoadScene(levelIndex);
        }
        private IEnumerator LoadCertainScene()
        {
            Time.timeScale = 1;
            transition.SetTrigger("Start");
            yield return new WaitForSeconds(transitionTime);
            SceneManager.LoadScene(certainScened);
        }
        public void QuitGame()
        {
            Application.Quit();
        }
        
    }
}