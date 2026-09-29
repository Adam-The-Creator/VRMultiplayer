using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppBootstrap : MonoBehaviour
{
    [SerializeField] private string firstScene = "Login";

    private IEnumerator Start()
    {
        Scene scene = SceneManager.GetSceneByName(firstScene);
        if (!scene.isLoaded) // already loaded when both scenes are open in the Editor
        {
            if (!Application.CanStreamedLevelBeLoaded(firstScene))
            {
                Debug.LogError($"[Bootstrap] '{firstScene}' is not in Build Settings!");
                yield break;
            }
            yield return SceneManager.LoadSceneAsync(firstScene, LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByName(firstScene);
        }
        SceneManager.SetActiveScene(scene);
    }
}