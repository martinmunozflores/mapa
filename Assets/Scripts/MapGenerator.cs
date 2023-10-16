using System.Collections;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode {NoiseMap, ColorMap, Mesh, Falloff};
    public DrawMode drawMode;

    public int mapWidth;
    public int mapHeight;
    public float heightMultiplier;
    public AnimationCurve heightCurve;
    public float noiseScale;

    public int octaves;
    public float persistance;
    public float lacunarity;

    public int seed;   
    public Vector2 offset;

    public bool autoUpdate;
    public bool useFalloff;
    public MeshFilter meshFilter; // Reference to the MeshFilter component of your mesh.
    public GameObject ship;
    public CameraFollow camera;

    [Range(0.0f, 1.0f)]
    public float a;
    [Range(0.0f, 1.0f)]
    public float b;
    
    public TerrainType[] regions;

    float[,] falloffMap;

    void Awake() {
		falloffMap = FalloffGen.GenerateFalloffMap(mapWidth, a, b);
	}

    void Start() {
        seed = Random.Range(0, 5000);
        GenerateMap();
        Vector3 pointToCheck = new Vector3(0f, 100f, -2000f); // Replace with the coordinates you want to check.
        float height = GetHeightAtPoint(pointToCheck);
        Debug.Log("Height at point: " + height);
        height += 10;
        GameObject myship = Instantiate(ship, new Vector3(0f, height, -2000f), Quaternion.identity);
        camera.target = myship.transform.Find("Turn").Find("Roll");
    }


    public void GenerateMap() {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight,seed, noiseScale, octaves, persistance, lacunarity, offset);
        Color[] colorMap = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth ; x++) {
                float currentHeight = 0;
                if (useFalloff) {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
                currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++) {
                    if (currentHeight <= regions[i].height) {
                        colorMap[y * mapWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap(noiseMap));
		} else if (drawMode == DrawMode.ColorMap) {
			display.DrawTexture (TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
		}else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, heightMultiplier, heightCurve), TextureGenerator.TextureFromColorMap(colorMap, mapWidth, mapHeight));
        }else if(drawMode == DrawMode.Falloff){
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGen.GenerateFalloffMap(mapWidth, a, b)));
        }
    }

    void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}

		falloffMap = FalloffGen.GenerateFalloffMap (mapWidth, a, b);
	}

    public float GetHeightAtPoint(Vector3 point)
    {
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter not assigned. Please assign a MeshFilter to the script.");
            return 0f;
        }

        Mesh mesh = meshFilter.mesh;

        // Create a ray from the point to a direction (e.g., straight down).
        Ray ray = new Ray(point + Vector3.up * 10f, Vector3.down);

        // Create a RaycastHit to store the result of the raycast.
        RaycastHit hit;

        // Perform the raycast against the mesh.
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // If the ray hits the mesh, return the Y coordinate of the hit point.
            return hit.point.y;
        }

        // If the ray doesn't hit the mesh, return a default value (e.g., 0).
        return -1f;
    }
    

}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}