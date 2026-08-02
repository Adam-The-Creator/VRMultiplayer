using UnityEngine;


public class SceneManagerHelper : MonoBehaviour
{
    private const string PlayerPrefShowSadChild = "ShowSadChild";
    private const string SadChildName = "SadChild";
    private const string PlayerPrefsShowSadGirl = "ShowSadGirl";
    private const string SadGirlName = "SadGirl";

    void Start()
    {
        bool showSadChild = PlayerPrefs.GetInt(PlayerPrefShowSadChild, 1) == 1;
        bool showSadGirl = PlayerPrefs.GetInt(PlayerPrefsShowSadGirl, 1) == 1;

        GameObject sadChild = GameObject.Find(SadChildName);
        
        if (sadChild != null)
        {
            sadChild.SetActive(showSadChild);
            Debug.Log($"SceneManagerHelper: Sad Child visibility set to: {showSadChild}");
        }
        else
        {
            Debug.LogWarning($"SceneManagerHelper: SadChild GameObject not found in scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'. This is normal if the scene doesn't have a Sad Child model.");
        }

        GameObject sadGirl = GameObject.Find(SadGirlName);
        if (sadGirl != null)
        {
            sadGirl.SetActive(showSadGirl);
            Debug.Log($"SceneManagerHelper: Sad Girl visibility set to: {showSadGirl}");
        }
        else
        {
            Debug.LogWarning($"SceneManagerHelper: SadGirl GameObject not found in scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'. This is normal if the scene doesn't have a Sad Girl model.");
        }
    }
}

