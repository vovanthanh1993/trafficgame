using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CoroutineRunner : MonoBehaviour
{
    
}
public static class GameCommonUtils 
{
    private static MonoBehaviour _coroutineRunner;
    
    private static MonoBehaviour CoroutineRunner
    {
        get
        {
            if (_coroutineRunner == null)
            {
                GameObject go = new GameObject("CoroutineRunner");
                _coroutineRunner = go.AddComponent<CoroutineRunner>();
                Object.DontDestroyOnLoad(go);
            }
            return _coroutineRunner;
        }
    }

    public static void LoadScene(string sceneName)
    {
        CoroutineRunner.StartCoroutine(LoadSceneAsync(sceneName));
    }

    private static IEnumerator LoadSceneAsync(string sceneName)
    {
        UIManager.Instance.ShowLoadingPanel(true);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            yield return null;
        }

        UIManager.Instance.ShowLoadingPanel(false);
    }

    // Get game time as string (mm:ss)
    public static string GetGameTimeString(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
