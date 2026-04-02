// 1st run this code
using UnityEngine;
using TMPro;

public class ShipInsideMonitor : MonoBehaviour
{
    private string text = "";

    void Update()
    {
        bool inShip = false;

        if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
        {
            inShip = GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom;
        }

        text = "IN SHIP: " + (inShip ? "YES" : "NO");
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(20, 20, 300, 50), text, style);
    }
}


// 2nd run this code

GameObject existing = GameObject.Find("ShipInsideMonitor");

if (existing != null)
{
    UnityEngine.Object.Destroy(existing);
    UnityEngine.Debug.Log("Stopped");
}
else
{
    GameObject go = new GameObject("ShipInsideMonitor");
    UnityEngine.Object.DontDestroyOnLoad(go);
    go.AddComponent<ShipInsideMonitor>();
    UnityEngine.Debug.Log("Started");
}

