// 1st run

using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

public class NSMonitor : MonoBehaviour
{
    public static NSMonitor Instance;

    private Harmony harmony;
    private Material sphereMat;
    private Material lineMat;

    private const float SphereLife = 3f;
    private const float HeardLineLife = 5f;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildMaterials();
        Patch();
        Debug.Log("[NSMonitor] 시작됨 - RoundManager.PlayAudibleNoise 기반");
    }

    void OnDestroy()
    {
        try { harmony?.UnpatchSelf(); } catch {}
        ClearVisuals();
        if (Instance == this) Instance = null;
        Debug.Log("[NSMonitor] 중지됨");
    }

    private void Patch()
    {
        harmony = new Harmony("unityexplorer.nsmonitor.noise.display");

        MethodInfo playNoise = AccessTools.Method(typeof(RoundManager), "PlayAudibleNoise");
        MethodInfo playNoisePatch = AccessTools.Method(typeof(NSMonitor), nameof(PlayAudibleNoisePrefix));
        harmony.Patch(playNoise, prefix: new HarmonyMethod(playNoisePatch));

        Type enemyCollisionType = AccessTools.TypeByName("EnemyAICollisionDetect");
        MethodInfo detectNoise = AccessTools.Method(enemyCollisionType, "INoiseListener.DetectNoise");
        MethodInfo detectNoisePatch = AccessTools.Method(typeof(NSMonitor), nameof(DetectNoisePrefix));

        if (detectNoise != null)
        {
            harmony.Patch(detectNoise, prefix: new HarmonyMethod(detectNoisePatch));
            Debug.Log("[NSMonitor] Enemy noise detect patch 성공");
        }
        else
        {
            Debug.LogWarning("[NSMonitor] EnemyAICollisionDetect.INoiseListener.DetectNoise 를 찾지 못함. 범위 표시는 작동하지만 몬스터 청각 라인은 안 나올 수 있음.");
        }
    }

    private void BuildMaterials()
    {
        Material baseMat = Resources.FindObjectsOfTypeAll<Material>()
            .FirstOrDefault(m => m != null && m.name == "fresnel_green");

        if (baseMat != null)
        {
            sphereMat = new Material(baseMat);
            sphereMat.color = new Color(1f, 0.55f, 0f, 0.22f);
        }
        else
        {
            Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            sphereMat = new Material(shader);
            sphereMat.color = new Color(1f, 0.55f, 0f, 0.22f);
        }

        lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = new Color(1f, 0.1f, 0.05f, 0.9f);
    }

    public static void PlayAudibleNoisePrefix(
        Vector3 noisePosition,
        float noiseRange,
        float noiseLoudness,
        int timesPlayedInSameSpot,
        bool noiseIsInsideClosedShip,
        int noiseID
    )
    {
        if (Instance == null) return;

        float actualRange = noiseIsInsideClosedShip ? noiseRange * 0.5f : noiseRange;
        Instance.SpawnNoiseSphere(noisePosition, actualRange, noiseLoudness, noiseID, timesPlayedInSameSpot);
    }

    public static void DetectNoisePrefix(
        object __instance,
        Vector3 noisePosition,
        float noiseLoudness,
        int timesNoisePlayedInOneSpot,
        int noiseID
    )
    {
        if (Instance == null || __instance == null) return;

        Component component = __instance as Component;
        if (component == null) return;

        object mainScriptObj = AccessTools.Field(__instance.GetType(), "mainScript")?.GetValue(__instance);
        Component enemy = mainScriptObj as Component;
        if (enemy == null) enemy = component;

        Instance.SpawnHeardLine(enemy.transform.position, noisePosition, noiseID, noiseLoudness);
    }

    private void SpawnNoiseSphere(Vector3 position, float range, float loudness, int noiseID, int times)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "NoiseSphere_actualRange";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * range * 2f;

        Collider col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        renderer.material = sphereMat;

        Destroy(sphere, SphereLife);

        Debug.Log($"[NoiseDebug] ID:{noiseID} pos:{position} range:{range:0.0} loud:{loudness:0.00} times:{times}");
    }

    private void SpawnHeardLine(Vector3 enemyPosition, Vector3 noisePosition, int noiseID, float loudness)
    {
        GameObject lineObj = new GameObject("NoiseLine_enemyHeard");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPosition(0, enemyPosition + Vector3.up * 0.7f);
        lr.SetPosition(1, noisePosition + Vector3.up * 0.15f);

        lr.startWidth = 0.045f;
        lr.endWidth = 0.025f;
        lr.useWorldSpace = true;
        lr.material = lineMat;
        lr.startColor = new Color(1f, 0.05f, 0.02f, 1f);
        lr.endColor = new Color(1f, 0.8f, 0.05f, 0.75f);

        Destroy(lineObj, HeardLineLife);

        Debug.Log($"[NoiseDebug] 몬스터가 소음 감지 ID:{noiseID} loud:{loudness:0.00}");
    }

    private void ClearVisuals()
    {
        GameObject[] all = FindObjectsOfType<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null &&
                (all[i].name.StartsWith("NoiseSphere_") || all[i].name.StartsWith("NoiseLine_")))
            {
                Destroy(all[i]);
            }
        }
    }
}







// 2nd run

UnityEngine.GameObject existing = UnityEngine.GameObject.Find("NSMonitor");

if (existing != null)
{
    UnityEngine.Object.Destroy(existing);
}
else
{
    UnityEngine.GameObject go = new UnityEngine.GameObject("NSMonitor");
    UnityEngine.Object.DontDestroyOnLoad(go);
    go.AddComponent<NSMonitor>();
}










