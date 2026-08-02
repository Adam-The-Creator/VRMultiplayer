using UnityEngine;

public class VRPlayer : MonoBehaviour
{ 
    void Start()
    {
        //TODO: Enable this to persist VRPlayer across scenes and solve issues with spatial panels
        //DontDestroyOnLoad(this.gameObject);
        
        Debug.Log("VRPlayer: Initialized and set to persist across scenes.");
    }
}
