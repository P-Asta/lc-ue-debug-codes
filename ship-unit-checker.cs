var shipPos = StartOfRound.Instance.shipLandingPosition.transform.position;
Log($"shipPos = {shipPos}");

var size = 40f;
var wallGroup = GameObject.Find("WallGroup");

if (wallGroup == null)
{
    wallGroup = new GameObject("WallGroup");

    var originalMat = Resources.FindObjectsOfTypeAll<Material>()
        .FirstOrDefault(m => m != null && m.name == "fresnel_green");

    Material fresnelMat = originalMat != null ? new Material(originalMat) : null;

    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    sphere.name = "DebugSphere";
    sphere.transform.parent = wallGroup.transform;
    sphere.transform.position = shipPos;
    sphere.transform.localScale = Vector3.one * size * 2;
    sphere.layer = LayerMask.NameToLayer("Terrain");

    UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());

    var renderer = sphere.GetComponent<Renderer>();

    // 그림자 / 조명 영향 제거
    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    renderer.receiveShadows = false;
    renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
    renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

    if (fresnelMat != null)
    {
        fresnelMat.color = new Color(0f, 0.5f, 1f, 1f);
        renderer.material = fresnelMat;
        Log("Fresnel 머티리얼 적용 성공!");
    }
    else
    {
        Log("fresnel_green 머티리얼을 찾지 못했습니다.");

        var fallback = new Material(Shader.Find("Sprites/Default"));
        fallback.color = new Color(0f, 0.5f, 1f, 0.3f);
        renderer.material = fallback;
    }
}
else
{
    UnityEngine.Object.Destroy(wallGroup);
}
