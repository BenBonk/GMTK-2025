﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Level {
    public class LevelLoader : MonoBehaviour
    {
        public Animator transition;
        public float transitionTime = 1f;
        private string certainScened;

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
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.R))
            {
                Restart();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                LoadNextLevel();
            }
        }
        public void QuitGame()
        {
            Application.Quit();
        }
        
    }
}