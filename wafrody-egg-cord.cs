var colors = new string[] {
	"#ea0064",  // HSLuv(  0, 100, 50) magenta
	"#b26300",  // HSLuv( 36, 100, 50) orange
	"#d4b800",  // HSLuv( 72, 100, 75) yellow
	"#5d8200",  // HSLuv(108, 100, 50) lime green
	"#005230",  // HSLuv(144, 100, 30) forest green
	"#00cfc0",  // HSLuv(180, 100, 75) cyan
	"#008398",  // HSLuv(216, 100, 50) blue
	"#003c71",  // HSLuv(252, 100, 25) dark blue
	"#be0cff",  // HSLuv(288, 100, 50) purple
	"#ff95e1"   // HSLuv(324, 100, 75) pink
}.Select(h => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.black).ToArray();

Action<GameObject, int> setupVisuals = (tile, xzSum) => {
	var material = tile.GetComponent<Renderer>().material;
	material.shader = Shader.Find("HDRP/Lit");
	var colorIndex = ((xzSum % colors.Length) + colors.Length) % colors.Length;
	material.color = colors[colorIndex];
};

var playerPos = StartOfRound.Instance.localPlayerController.gameObject.transform.position;
Log($"playerPos = {playerPos}");

const int radius = 14;
int startX = (int)playerPos.x - radius;
int startZ = (int)playerPos.z - radius;
float tileY = playerPos.y;

var tileGroup = GameObject.Find("TileGroup");
if (tileGroup == null) {
	Log("TileGroup not found, creating a new one");
	tileGroup = new GameObject("TileGroup");

	for (int offsetX = 0; offsetX < 2 * radius; offsetX++) {
		for (int offsetZ = 0; offsetZ < 2 * radius; offsetZ++) {
			var tileX = startX + offsetX;
			var tileZ = startZ + offsetZ;
			var pos = new Vector3((float)tileX + 0.5f, tileY - 0.03f, (float)tileZ + 0.5f);
			var coordSum = (int)pos.x + (int)pos.z;

			var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
			tile.name = $"Tile_{tileX}x_{tileZ}z";
			tile.transform.parent = tileGroup.transform;
			tile.transform.position = pos;
			tile.transform.localScale = new Vector3(0.98f, 0.01f, 0.98f);
			setupVisuals(tile, coordSum);
		}
	}
} else {
	var children = new List<GameObject>();
	foreach (var childTransform in tileGroup.transform) children.Add(((Transform)childTransform).gameObject);
	foreach (var child in children) UnityEngine.Object.Destroy((GameObject)child);
	UnityEngine.Object.Destroy(tileGroup);
}

foreach (var item in UnityEngine.Object.FindObjectsOfType<StunGrenadeItem>()) {
	Log($"{item.name} at {item.transform.position}");
}
foreach (var item in UnityEngine.Object.FindObjectsOfType<FlashlightItem>()) {
	Log($"{item.name} at {item.transform.position}");
}
