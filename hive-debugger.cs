var CreateDebugSpheres = new System.Action<GameObject, float[], Color[]>((target, sizes, colors) =>
{
    var wallGroup = target.transform.Find("WallGroup")?.gameObject;

    if (wallGroup == null)
    {
        wallGroup = new GameObject("WallGroup");
        wallGroup.transform.parent = target.transform;
        wallGroup.transform.localPosition = Vector3.zero;

        var originalMat = Resources.FindObjectsOfTypeAll<Material>()
            .FirstOrDefault(m => m != null && m.name == "fresnel_green");

        for (int i = 0; i < sizes.Length; i++)
        {
            float size = sizes[i];
            Color color = colors[i];

            Material fresnelMat = originalMat != null ? new Material(originalMat) : null;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "DebugSphere_" + size + "u";
            sphere.transform.parent = wallGroup.transform;
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * size * 2;

            UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());

            if (fresnelMat != null)
            {
                fresnelMat.color = color;
                sphere.GetComponent<Renderer>().material = fresnelMat;
            }
        }

        Log(target.name + " - 구 생성 완료");
    }
    else
    {
        UnityEngine.Object.Destroy(wallGroup);
    }
});


var hives = GameObject.FindObjectsOfType<GameObject>()
    .Where(g => g.name.Contains("RedLocustHive"));

var bees = GameObject.FindObjectsOfType<GameObject>()
    .Where(g => g.name.Contains("RedLocustBee"));

foreach (var hive in hives)
{
    CreateDebugSpheres(
        hive,
        new float[] { 10f, 15f },
        new Color[] {
            new Color(0f,0.5f,1f,1f),
            new Color(1f,0.2f,0f,1f)
        });
}

foreach (var bee in bees)
{
    CreateDebugSpheres(
        bee,
        new float[] { 16f, 21f },
        new Color[] {
            new Color(1f, 1f, 1f, 1f),
            new Color(0f, 0f, 0f, 0f),
        }
    );
}
