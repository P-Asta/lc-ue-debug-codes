// 1st run this cod
using System.Reflection;
using System.Text;
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

    string BuildCurrentTileInfo()
    {
        if (StartOfRound.Instance == null ||
            StartOfRound.Instance.occlusionCuller == null ||
            StartOfRound.Instance.occlusionCuller.currentTile == null)
        {
            return "No current tile";
        }

        var currentTile = StartOfRound.Instance.occlusionCuller.currentTile;
        int growthTileIndex = -1;

        for (int i = 0; i < cadaverAI.GrowthTiles.Count; i++)
        {
            if (cadaverAI.GrowthTiles[i].tile == currentTile)
            {
                growthTileIndex = i;
                break;
            }
        }

        if (growthTileIndex == -1)
            return "Current tile has no growth entry";

        var growthTile = cadaverAI.GrowthTiles[growthTileIndex];

        int batchedExactCount = 0;
        int[] typeCounts = null;

        if (cadaverAI.plantBatchers != null)
        {
            typeCounts = new int[cadaverAI.plantBatchers.Length];

            for (int plantType = 0; plantType < cadaverAI.plantBatchers.Length; plantType++)
            {
                var batcher = cadaverAI.plantBatchers[plantType];
                if (batcher == null || batcher.batchedPositionTiles == null)
                    continue;

                for (int i = 0; i < batcher.batchedPositionTiles.Count; i++)
                {
                    if (batcher.batchedPositionTiles[i] == growthTileIndex)
                    {
                        batchedExactCount++;
                        typeCounts[plantType]++;
                    }
                }
            }
        }

        float eradicatedElapsed = 0f;
        float eradicatedLeft = 0f;
        if (growthTile.eradicated)
        {
            eradicatedElapsed = Time.realtimeSinceStartup - growthTile.eradicatedAtTime;
            eradicatedLeft = Mathf.Max(0f, 45f - eradicatedElapsed);
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("tileIndex      : ").Append(growthTileIndex);
        sb.Append("\nplantsInTile   : ").Append(growthTile.plantsInTile);
        sb.Append("\nplantPositions : ").Append(growthTile.plantPositions != null ? growthTile.plantPositions.Count : 0);
        sb.Append("\nbatchedExact   : ").Append(batchedExactCount);
        sb.Append("\neradicated     : ").Append(growthTile.eradicated);
        sb.Append("\neradicatedAt   : ").Append(growthTile.eradicatedAtTime.ToString("F2"));
        sb.Append("\neradicatedLeft : ").Append(growthTile.eradicated ? eradicatedLeft.ToString("F2") + "s" : "inactive");
        sb.Append("\ncannotSpread   : ").Append(growthTile.cannotSpread);

        bool mismatch =
            growthTile.plantsInTile != batchedExactCount ||
            growthTile.plantsInTile != (growthTile.plantPositions != null ? growthTile.plantPositions.Count : 0);

        sb.Append("\ncountMismatch  : ").Append(mismatch);

        if (typeCounts != null)
        {
            sb.Append("\ntypeBreakdown  : ");
            bool first = true;

            for (int i = 0; i < typeCounts.Length; i++)
            {
                if (typeCounts[i] <= 0) continue;

                if (!first) sb.Append(", ");
                sb.Append("T").Append(i).Append("=").Append(typeCounts[i]);
                first = false;
            }

            if (first)
                sb.Append("none");
        }

        return sb.ToString();
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
        string tileInfo = BuildCurrentTileInfo();

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
            + "\n\n=== Current Tile ===\n"
            + tileInfo;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.green;
        style.alignment = TextAnchor.UpperLeft;
        style.wordWrap = false;
    
        GUIStyle bgStyle = new GUIStyle(GUI.skin.box);
        bgStyle.normal.background = Texture2D.blackTexture;
    
        float width = 620f;
        float height = Screen.height - 20f;
    
        GUI.Box(new Rect(10, 10, width, height), "", bgStyle);
    
        Rect viewRect = new Rect(20, 15, width - 30f, 2000f);
        GUI.Label(viewRect, displayText, style);
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

