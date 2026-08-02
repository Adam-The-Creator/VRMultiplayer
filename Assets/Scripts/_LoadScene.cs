using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class _LoadScene : MonoBehaviour
{
    // TEST SCRIPT TO LOAD A SCENE, DELETE THIS LATER

    public Button loadSceneBtn;
    public string sceneToLoad = "";

    void Start()
    {
        if (string.IsNullOrEmpty(sceneToLoad)) Debug.LogError("Scene to load is not specified");
        loadSceneBtn.onClick.AddListener(() => StartCoroutine(LoadSceneWithDelay(sceneToLoad)));
    }

    private IEnumerator LoadSceneWithDelay(string sceneName)
    {
        loadSceneBtn.interactable = false;

        //if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        yield return new WaitForSeconds(0.2f);

        SceneManager.LoadScene(sceneName);
    }
}
