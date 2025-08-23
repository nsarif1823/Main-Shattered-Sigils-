using UnityEngine;

namespace Aeloria.Environment
{
    /// <summary>
    /// Creates a grid floor for testing isometric movement
    /// Helps visualize the isometric perspective
    /// </summary>
    public class GridFloor : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridSizeX = 20;
        [SerializeField] private int gridSizeZ = 20;
        [SerializeField] private float tileSize = 1f;

        [Header("Visuals")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Material lightTileMaterial;
        [SerializeField] private Material darkTileMaterial;
        [SerializeField] private bool createCheckerboard = true;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool centerGrid = true;

        private GameObject[,] tiles;

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateFloor();
            }
        }

        /// <summary>
        /// Generate the grid floor
        /// </summary>
        public void GenerateFloor()
        {
            ClearFloor();

            tiles = new GameObject[gridSizeX, gridSizeZ];

            // Calculate offset to center the grid
            Vector3 offset = Vector3.zero;
            if (centerGrid)
            {
                offset = new Vector3(
                    -(gridSizeX * tileSize) / 2f + tileSize / 2f,
                    0,
                    -(gridSizeZ * tileSize) / 2f + tileSize / 2f
                );
            }

            // Create tiles
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    CreateTile(x, z, offset);
                }
            }
        }

        /// <summary>
        /// Create a single tile
        /// </summary>
        private void CreateTile(int x, int z, Vector3 offset)
        {
            Vector3 position = new Vector3(x * tileSize, 0, z * tileSize) + offset;

            GameObject tile;

            if (tilePrefab != null)
            {
                tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
            }
            else
            {
                // Create a simple cube if no prefab provided
                tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.transform.position = position;
                tile.transform.localScale = new Vector3(tileSize, 0.1f, tileSize);
                tile.transform.parent = transform;
            }

            tile.name = $"Tile_{x}_{z}";

            // Apply checkerboard material
            if (createCheckerboard)
            {
                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    bool isLight = (x + z) % 2 == 0;

                    if (isLight && lightTileMaterial != null)
                    {
                        renderer.material = lightTileMaterial;
                    }
                    else if (!isLight && darkTileMaterial != null)
                    {
                        renderer.material = darkTileMaterial;
                    }
                }
            }

            tiles[x, z] = tile;
        }

        /// <summary>
        /// Clear all existing tiles
        /// </summary>
        public void ClearFloor()
        {
            if (tiles != null)
            {
                foreach (GameObject tile in tiles)
                {
                    if (tile != null)
                    {
                        if (Application.isPlaying)
                            Destroy(tile);
                        else
                            DestroyImmediate(tile);
                    }
                }
            }

            // Clear any remaining children
            while (transform.childCount > 0)
            {
                if (Application.isPlaying)
                    Destroy(transform.GetChild(0).gameObject);
                else
                    DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }

        /// <summary>
        /// Get tile at grid position
        /// </summary>
        public GameObject GetTileAt(int x, int z)
        {
            if (tiles == null || x < 0 || x >= gridSizeX || z < 0 || z >= gridSizeZ)
                return null;

            return tiles[x, z];
        }

        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / tileSize);
            int z = Mathf.RoundToInt(worldPos.z / tileSize);
            return new Vector2Int(x, z);
        }
    }
}