// 1st run this code

using System.Reflection;
using GameNetcodeStuff;
using UnityEngine;

public class InfectionMonitorBehaviour : MonoBehaviour
{
    public float interval = 0.1f;

    private CadaverGrowthAI cadaverAI;
    private float timer = 0f;
    private string displayText = "";

    private FieldInfo totalTimeSpentInPlantsField;
    private FieldInfo localPlayerImmunityTimerField;
    private FieldInfo stoodInWeedsLastCheckField;
    private FieldInfo numberOfInfectedField;

    void CacheReflection()
    {
        if (totalTimeSpentInPlantsField == null)
            totalTimeSpentInPlantsField = typeof(CadaverGrowthAI).GetField("totalTimeSpentInPlants", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (localPlayerImmunityTimerField == null)
            localPlayerImmunityTimerField = typeof(CadaverGrowthAI).GetField("localPlayerImmunityTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        if (stoodInWeedsLastCheckField == null)
            stoodInWeedsLastCheckField = typeof(CadaverGrowthAI).GetField("stoodInWeedsLastCheck", BindingFlags.Instance | BindingFlags.NonPublic);

        if (numberOfInfectedField == null)
            numberOfInfectedField = typeof(CadaverGrowthAI).GetField("numberOfInfected", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    float GetPrivateFloat(FieldInfo field)
    {
        if (field == null || cadaverAI == null) return 0f;
        return (float)field.GetValue(cadaverAI);
    }

    bool GetPrivateBool(FieldInfo field)
    {
        if (field == null || cadaverAI == null) return false;
        return (bool)field.GetValue(cadaverAI);
    }

    int GetPrivateInt(FieldInfo field)
    {
        if (field == null || cadaverAI == null) return 0;
        return (int)field.GetValue(cadaverAI);
    }

    string GetInfectionStage(PlayerInfection inf)
    {
        if (!inf.infected) return "Not infected";
        if (inf.burstMeter >= 1f) return "Bursting";
        if (inf.infectionMeter >= 1f) return "Burst phase";
        if (inf.burstMeter >= 0.9f) return "Imminent burst";
        if (inf.infectionMeter > 0.8f) return "High fever";
        if (inf.infectionMeter > 0.6f) return "Cough active";
        if (inf.infectionMeter > 0.55f) return "Bloom-on-death active";
        if (inf.infectionMeter > 0f) return "Infected";
        return "Unknown";
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < interval) return;
        timer = 0f;

        if (cadaverAI == null)
        {
            cadaverAI = GameObject.FindObjectOfType<CadaverGrowthAI>();
            if (cadaverAI == null)
            {
                displayText = "[InfectionMonitor] CadaverGrowthAI not found";
                return;
            }

            CacheReflection();
        }

        PlayerControllerB local = GameNetworkManager.Instance.localPlayerController;
        if (local == null)
        {
            displayText = "[InfectionMonitor] local player not found";
            return;
        }

        int id = (int)local.playerClientId;
        PlayerInfection inf = cadaverAI.playerInfections[id];

        float exposureTimer = GetPrivateFloat(totalTimeSpentInPlantsField);
        float immunityTimer = GetPrivateFloat(localPlayerImmunityTimerField);
        bool stoodInWeedsLastCheck = GetPrivateBool(stoodInWeedsLastCheckField);
        int numberOfInfected = GetPrivateInt(numberOfInfectedField);

        bool hasFaceSpores = inf.faceSpores != null;
        bool hasBackFlowers = inf.backFlowers != null;
        bool coughBaseReady = inf.infected && inf.infectionMeter > 0.6f;
        bool coughVisualsReady = hasFaceSpores && hasBackFlowers;
        bool coughAlwaysReady = coughBaseReady && inf.emittingSpores && coughVisualsReady;
        bool coughOnePercentReady = coughBaseReady && !inf.emittingSpores && coughVisualsReady;

        string stage = GetInfectionStage(inf);

        string tileInfo = "No current tile";
        if (StartOfRound.Instance != null &&
            StartOfRound.Instance.occlusionCuller != null &&
            StartOfRound.Instance.occlusionCuller.currentTile != null)
        {
            var currentTile = StartOfRound.Instance.occlusionCuller.currentTile;
            bool foundTile = false;

            for (int i = 0; i < cadaverAI.GrowthTiles.Count; i++)
            {
                var growthTile = cadaverAI.GrowthTiles[i];
                if (growthTile.tile == currentTile)
                {
                    foundTile = true;
                    tileInfo =
                        "plants=" + growthTile.plantsInTile
                        + ", eradicated=" + growthTile.eradicated
                        + ", positions=" + growthTile.plantPositions.Count;
                    break;
                }
            }

            if (!foundTile)
                tileInfo = "Current tile has no growth entry";
        }

        bool solo = StartOfRound.Instance.connectedPlayersAmount == 0;
        float infectionGate = solo ? 7f : 3f;

        displayText =
            "=== Infection Monitor ==="
            + "\nmode          : " + (solo ? "Solo" : "Multi")
            + "\nstage         : " + stage
            + "\ninfected      : " + inf.infected
            + "\ninfectionMeter: " + (inf.infectionMeter * 100f).ToString("F1") + "%"
            + "\nburstMeter    : " + (inf.burstMeter * 100f).ToString("F1") + "%"
            + "\nsevere        : " + inf.severe
            + "\nemittingSpores: " + inf.emittingSpores
            + "\nbloomOnDeath  : " + inf.bloomOnDeath
            + "\nhealing       : " + inf.healing
            + "\nmultiplier    : " + inf.multiplier.ToString("F2")
            + "\nhindering     : " + inf.hinderingPlayerMovement
            + "\nfaceSpores    : " + hasFaceSpores
            + "\nbackFlowers   : " + hasBackFlowers
            + "\ncanCoughBase  : " + coughBaseReady
            + "\ncanCoughNow   : " + coughAlwaysReady
            + "\ncanCough1pct  : " + coughOnePercentReady
            + "\ninsideFactory : " + local.isInsideFactory
            + "\npoison        : " + local.poison.ToString("F2")
            + "\noverridePoison: " + local.overridePoisonValue
            + "\nmoveHindered  : " + local.isMovementHindered
            + "\ntimeSpentInPlants : " + exposureTimer.ToString("F2")
            + "\nfilterTimer   : " + immunityTimer.ToString("F2") + " / " + infectionGate.ToString("F0")
            + "\nstoodInWeeds  : " + stoodInWeedsLastCheck
            + "\nnumInfected   : " + numberOfInfected
            + "\ncurrentTile   : " + tileInfo;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.green;
        style.alignment = TextAnchor.UpperLeft;

        GUIStyle bgStyle = new GUIStyle(GUI.skin.box);
        bgStyle.normal.background = Texture2D.blackTexture;

        GUI.Box(new Rect(10, 10, 460, 500), "", bgStyle);
        GUI.Label(new Rect(20, 15, 440, 490), displayText, style);
    }
}













// 2nd run this code
GameObject existing = GameObject.Find("InfectionMonitor");
if (existing != null)
{
    GameObject.Destroy(existing);
    Debug.Log("[InfectionMonitor] Stopped");
}
else
{
    GameObject go = new GameObject("InfectionMonitor");
    GameObject.DontDestroyOnLoad(go);
    go.AddComponent<InfectionMonitorBehaviour>();
    Debug.Log("[InfectionMonitor] Started");
}
