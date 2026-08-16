//using System.Collections;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.EventSystems;

//public class Setup : MonoBehaviour
//{
//    [Tooltip("The first scene to load")]
//    private static readonly string firstSceneName = "MenuScene";

//    [SerializeField]
//    private GameObject xrCore;

//    [SerializeField, Tooltip("GameObject of the TemporaryPlayer")]
//    private GameObject temporaryPlayer;

//    [SerializeField, Tooltip("Gameobject of the NetworkManager")]
//    private GameObject networkManager;

//    void Awake()
//    {
//        DatabaseManager.Instance.Initialize();

//        if (xrCore != null) DontDestroyOnLoad(xrCore);
//        else Debug.LogWarning("[Setup] XR Core is not assigned.");

//        if (temporaryPlayer != null) DontDestroyOnLoad(temporaryPlayer);
//        else Debug.LogWarning("[Setup] Temporary Player Prefab is not assigned.");

//        if (networkManager != null)
//        {
//            DontDestroyOnLoad(networkManager);
//            networkManager.SetActive(false);
//        }
//        else Debug.LogWarning("[Setup] Network Manager Prefab is not assigned.");
//    }

//    void Start()
//    {
//        StartCoroutine(LoadFirstSceneWithDelay());
//    }

//    private IEnumerator LoadFirstSceneWithDelay()
//    {
//        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
//        yield return new WaitForSeconds(0.2f);
//        SceneManager.LoadScene(firstSceneName);
//    }
//}
