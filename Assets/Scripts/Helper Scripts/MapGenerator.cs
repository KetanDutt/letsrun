using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class MapGenerator : MonoBehaviour
{
    public static MapGenerator instance;

    // Field names are deliberately retained: the authored Gameplay scene serializes them.
    public GameObject roadPrefab, grass_Prefab, groundPrefab_1, groundPrefab_2, groundPrefab_3,
        groundPrefab_4, grass_Bottom_Prefab, land_Prefab_1, land_Prefab_2, land_Prefab_3,
        land_Prefab_4, land_Prefab_5, big_Grass_Prefab, big_Grass_Bottom_Prefab,
        treePrefab_1, treePrefab_2, treePrefab_3, big_Tree_Prefab;
    public GameObject road_Holder, top_Near_Side_Walk_Holder, top_Far_Side_Walk_Holder,
        bottom_Near_Side_Walk_Holder, bottom_Far_Side_Walk_Holder;
    public int start_Road_Tile, start_Grass_Tile, start_Ground3_Tile, start_Land_Tile;
    public List<GameObject> road_Tiles, top_Near_Grass_Tiles, top_Far_Grass_Tiles,
        bottom_Near_Grass_Tiles, bottom_Far_Land_F1_Tiles, bottom_Far_Land_F2_Tiles,
        bottom_Far_Land_F3_Tiles, bottom_Far_Land_F4_Tiles, bottom_Far_Land_F5_Tiles;
    public int[] pos_For_Top_Ground_1, pos_For_Top_Ground_2, pos_For_Top_Ground_4,
        pos_For_Top_Big_Grass, pos_For_Top_Tree_1, pos_For_Top_Tree_2, pos_For_Top_Tree_3,
        pos_For_Bottom_Big_Grass, pos_For_Bottom_Tree1, pos_For_Bottom_Tree2, pos_For_Bottom_Tree3;
    public int pos_For_Road_Tile_1, pos_For_Road_Tile_2, pos_For_Road_Tile_3;

    [HideInInspector] public Vector3 last_Pos_Of_Road_Tile, last_Pos_Of_Top_Near_Grass,
        last_Pos_Of_Top_Far_Grass, last_Pos_Of_Bottom_Near_Grass, last_Pos_Of_Botom_Far_Land_F1,
        last_Pos_Of_Botom_Far_Land_F2, last_Pos_Of_Botom_Far_Land_F3,
        last_Pos_Of_Botom_Far_Land_F4, last_Pos_Of_Botom_Far_Land_F5;
    [HideInInspector] public int last_Order_Of_Road, last_Order_Of_Top_Near_Grass,
        last_Order_Of_Top_Far_Grass, last_Order_Of_Bottom_Near_Grass,
        last_Order_Of_Bottom_Far_Land_F1, last_Order_Of_Bottom_Far_Land_F2,
        last_Order_Of_Bottom_Far_Land_F3, last_Order_Of_Bottom_Far_Land_F4, last_Order_Of_Bottom_Far_Land_F5;

    public Plane[] VisibilityPlanes { get { return planes; } }
    public bool HasVisibility { get; private set; }
    public float CameraX { get { return view == null ? 0f : view.transform.position.x; } }
    private readonly Plane[] planes = new Plane[6];
    private Camera view;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        if (instance != null && instance != this) { enabled = false; Destroy(gameObject); return; }
        instance = this;
        view = Camera.main;
    }

    private void Start()
    {
        if (!ValidateReferences()) { enabled = false; return; }
        InitializePlatform(roadPrefab, ref last_Pos_Of_Road_Tile, roadPrefab.transform.position,
            start_Road_Tile, road_Holder, ref road_Tiles, ref last_Order_Of_Road, new Vector3(1.5f, 0f));
        InitializePlatform(grass_Prefab, ref last_Pos_Of_Top_Near_Grass, grass_Prefab.transform.position,
            start_Grass_Tile, top_Near_Side_Walk_Holder, ref top_Near_Grass_Tiles, ref last_Order_Of_Top_Near_Grass, new Vector3(1.2f, 0f));
        InitializePlatform(groundPrefab_3, ref last_Pos_Of_Top_Far_Grass, groundPrefab_3.transform.position,
            start_Ground3_Tile, top_Far_Side_Walk_Holder, ref top_Far_Grass_Tiles, ref last_Order_Of_Top_Far_Grass, new Vector3(4.8f, 0f));
        InitializePlatform(grass_Bottom_Prefab, ref last_Pos_Of_Bottom_Near_Grass,
            new Vector3(2f, grass_Bottom_Prefab.transform.position.y), start_Grass_Tile,
            bottom_Near_Side_Walk_Holder, ref bottom_Near_Grass_Tiles, ref last_Order_Of_Bottom_Near_Grass, new Vector3(1.2f, 0f));
        InitializePlatform(land_Prefab_1, ref last_Pos_Of_Botom_Far_Land_F1, land_Prefab_1.transform.position,
            start_Land_Tile, bottom_Far_Side_Walk_Holder, ref bottom_Far_Land_F1_Tiles, ref last_Order_Of_Bottom_Far_Land_F1, new Vector3(1.6f, 0f));
        InitializePlatform(land_Prefab_2, ref last_Pos_Of_Botom_Far_Land_F2, land_Prefab_2.transform.position,
            start_Land_Tile - 3, bottom_Far_Side_Walk_Holder, ref bottom_Far_Land_F2_Tiles, ref last_Order_Of_Bottom_Far_Land_F2, new Vector3(1.6f, 0f));
        InitializePlatform(land_Prefab_3, ref last_Pos_Of_Botom_Far_Land_F3, land_Prefab_3.transform.position,
            start_Land_Tile - 4, bottom_Far_Side_Walk_Holder, ref bottom_Far_Land_F3_Tiles, ref last_Order_Of_Bottom_Far_Land_F3, new Vector3(1.6f, 0f));
        InitializePlatform(land_Prefab_4, ref last_Pos_Of_Botom_Far_Land_F4, land_Prefab_4.transform.position,
            start_Land_Tile - 7, bottom_Far_Side_Walk_Holder, ref bottom_Far_Land_F4_Tiles, ref last_Order_Of_Bottom_Far_Land_F4, new Vector3(1.6f, 0f));
        InitializePlatform(land_Prefab_5, ref last_Pos_Of_Botom_Far_Land_F5, land_Prefab_5.transform.position,
            start_Land_Tile - 10, bottom_Far_Side_Walk_Holder, ref bottom_Far_Land_F5_Tiles, ref last_Order_Of_Bottom_Far_Land_F5, new Vector3(1.6f, 0f));
    }

    private bool ValidateReferences()
    {
        GameObject[] prefabs = { roadPrefab, grass_Prefab, groundPrefab_1, groundPrefab_2, groundPrefab_3,
            groundPrefab_4, grass_Bottom_Prefab, land_Prefab_1, land_Prefab_2, land_Prefab_3,
            land_Prefab_4, land_Prefab_5, big_Grass_Prefab, big_Grass_Bottom_Prefab,
            treePrefab_1, treePrefab_2, treePrefab_3, big_Tree_Prefab };
        for (int i = 0; i < prefabs.Length; i++)
            if (prefabs[i] == null || prefabs[i].GetComponent<SpriteRenderer>() == null)
            {
                Debug.LogError("Map Generator has a missing prefab or SpriteRenderer at slot " + i + ".");
                return false;
            }
        if (road_Holder == null || top_Near_Side_Walk_Holder == null || top_Far_Side_Walk_Holder == null ||
            bottom_Near_Side_Walk_Holder == null || bottom_Far_Side_Walk_Holder == null)
        {
            Debug.LogError("Assign all five map holders on Map Generator.");
            return false;
        }
        return true;
    }

    private void InitializePlatform(GameObject prefab, ref Vector3 nextPosition, Vector3 firstPosition,
        int count, GameObject holder, ref List<GameObject> tiles, ref int nextOrder, Vector3 step)
    {
        count = Mathf.Max(1, count);
        if (tiles == null) tiles = new List<GameObject>(count);
        else tiles.Clear();
        nextPosition = firstPosition;
        for (int i = 0; i < count; i++)
        {
            GameObject tile = Instantiate(prefab, nextPosition, prefab.transform.rotation, holder.transform);
            tile.GetComponent<SpriteRenderer>().sortingOrder = i;
            if (tile.CompareTag(MyTags.TOP_NEAR_GRASS))
                SetNearScene(big_Grass_Prefab, tile, i, pos_For_Top_Big_Grass, pos_For_Top_Tree_1, pos_For_Top_Tree_2, pos_For_Top_Tree_3);
            else if (tile.CompareTag(MyTags.BOTTOM_NEAR_GRASS))
                SetNearScene(big_Grass_Bottom_Prefab, tile, i, pos_For_Bottom_Big_Grass, pos_For_Bottom_Tree1, pos_For_Bottom_Tree2, pos_For_Bottom_Tree3);
            else if (tile.CompareTag(MyTags.BOTTOM_FAR_LAND_2) && i == 5)
                CreateDecoration(big_Tree_Prefab, tile, new Vector3(-0.57f, -1.34f));
            else if (tile.CompareTag(MyTags.TOP_FAR_GRASS))
            {
                if (Contains(pos_For_Top_Ground_1, i)) CreateDecoration(groundPrefab_1, tile, Vector3.zero, true);
                if (Contains(pos_For_Top_Ground_2, i)) CreateDecoration(groundPrefab_2, tile, Vector3.zero, true);
                if (Contains(pos_For_Top_Ground_4, i)) CreateDecoration(groundPrefab_4, tile, Vector3.zero, true);
            }
            tiles.Add(tile);
            nextPosition += step;
        }
        nextOrder = count;
    }

    private static bool Contains(int[] positions, int index) { return positions != null && Array.IndexOf(positions, index) >= 0; }

    private void SetNearScene(GameObject bigGrass, GameObject tile, int index, int[] big, int[] first, int[] second, int[] third)
    {
        if (Contains(big, index))
        {
            GameObject grass = CreateDecoration(bigGrass, tile, new Vector3(-0.183f, 0.106f), true);
            CreateDecoration(treePrefab_1, grass, new Vector3(0f, 1.52f));
        }
        if (Contains(first, index)) CreateDecoration(treePrefab_1, tile, new Vector3(0f, 1.15f));
        if (Contains(second, index)) CreateDecoration(treePrefab_2, tile, new Vector3(0f, 1.15f));
        if (Contains(third, index)) CreateDecoration(treePrefab_3, tile, new Vector3(0f, 1.15f));
    }

    private static GameObject CreateDecoration(GameObject prefab, GameObject tile, Vector3 localPosition, bool replaceTile = false)
    {
        GameObject decoration = Instantiate(prefab, tile.transform.position, prefab.transform.rotation, tile.transform);
        decoration.transform.localPosition = localPosition;
        SpriteRenderer parentRenderer = tile.GetComponent<SpriteRenderer>();
        decoration.GetComponent<SpriteRenderer>().sortingOrder = parentRenderer.sortingOrder;
        if (replaceTile) parentRenderer.enabled = false;
        return decoration;
    }

    private void LateUpdate()
    {
        HasVisibility = view != null;
        if (HasVisibility) GeometryUtility.CalculateFrustumPlanes(view, planes); // One non-allocating query for every tile.
        NormalizeOrders(road_Tiles, ref last_Order_Of_Road);
        NormalizeOrders(top_Near_Grass_Tiles, ref last_Order_Of_Top_Near_Grass);
        NormalizeOrders(top_Far_Grass_Tiles, ref last_Order_Of_Top_Far_Grass);
        NormalizeOrders(bottom_Near_Grass_Tiles, ref last_Order_Of_Bottom_Near_Grass);
        NormalizeOrders(bottom_Far_Land_F1_Tiles, ref last_Order_Of_Bottom_Far_Land_F1);
        NormalizeOrders(bottom_Far_Land_F2_Tiles, ref last_Order_Of_Bottom_Far_Land_F2);
        NormalizeOrders(bottom_Far_Land_F3_Tiles, ref last_Order_Of_Bottom_Far_Land_F3);
        NormalizeOrders(bottom_Far_Land_F4_Tiles, ref last_Order_Of_Bottom_Far_Land_F4);
        NormalizeOrders(bottom_Far_Land_F5_Tiles, ref last_Order_Of_Bottom_Far_Land_F5);
    }

    private static void NormalizeOrders(List<GameObject> tiles, ref int nextOrder)
    {
        if (nextOrder < 4096 || tiles == null || tiles.Count == 0) return;
        int minimum = int.MaxValue;
        for (int i = 0; i < tiles.Count; i++) minimum = Mathf.Min(minimum, tiles[i].GetComponent<SpriteRenderer>().sortingOrder);
        if (minimum <= 0) return;
        for (int i = 0; i < tiles.Count; i++)
        {
            OffScreen recycler = tiles[i].GetComponent<OffScreen>();
            if (recycler != null) recycler.OffsetSortingOrder(-minimum);
        }
        nextOrder -= minimum; // Avoid Unity's signed 16-bit sorting-order limit during long runs.
    }

    public void ShiftOrigin(Vector3 offset)
    {
        ShiftTiles(road_Tiles, offset); ShiftTiles(top_Near_Grass_Tiles, offset);
        ShiftTiles(top_Far_Grass_Tiles, offset); ShiftTiles(bottom_Near_Grass_Tiles, offset);
        ShiftTiles(bottom_Far_Land_F1_Tiles, offset); ShiftTiles(bottom_Far_Land_F2_Tiles, offset);
        ShiftTiles(bottom_Far_Land_F3_Tiles, offset); ShiftTiles(bottom_Far_Land_F4_Tiles, offset); ShiftTiles(bottom_Far_Land_F5_Tiles, offset);
        last_Pos_Of_Road_Tile -= offset; last_Pos_Of_Top_Near_Grass -= offset;
        last_Pos_Of_Top_Far_Grass -= offset; last_Pos_Of_Bottom_Near_Grass -= offset;
        last_Pos_Of_Botom_Far_Land_F1 -= offset; last_Pos_Of_Botom_Far_Land_F2 -= offset;
        last_Pos_Of_Botom_Far_Land_F3 -= offset; last_Pos_Of_Botom_Far_Land_F4 -= offset; last_Pos_Of_Botom_Far_Land_F5 -= offset;
    }

    private static void ShiftTiles(List<GameObject> tiles, Vector3 offset)
    {
        if (tiles == null) return;
        // Keep holders at the origin too; otherwise local coordinates would still grow without bound.
        for (int i = 0; i < tiles.Count; i++) if (tiles[i] != null) tiles[i].transform.position -= offset;
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
