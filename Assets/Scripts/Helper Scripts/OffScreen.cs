using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class OffScreen : MonoBehaviour
{
    private SpriteRenderer rootRenderer;
    private SpriteRenderer[] renderers;
    private Vector3 boundsOffset, boundsSize;

    private void Awake() { rootRenderer = GetComponent<SpriteRenderer>(); }

    private void Start()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        Bounds bounds = rootRenderer.bounds;
        for (int i = 0; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        // Tile decorations are static relative to their root. Cache their full extent once.
        boundsOffset = bounds.center - transform.position;
        boundsSize = bounds.size;
    }

    private void LateUpdate()
    {
        MapGenerator map = MapGenerator.instance;
        if (map == null || !map.HasVisibility || renderers == null) return;
        if (transform.position.x >= map.CameraX) return;
        var bounds = new Bounds(transform.position + boundsOffset, boundsSize);
        if (GeometryUtility.TestPlanesAABB(map.VisibilityPlanes, bounds)) return;

        if (CompareTag(MyTags.ROAD))
            Recycle(ref map.last_Pos_Of_Road_Tile, 1.5f, ref map.last_Order_Of_Road);
        else if (CompareTag(MyTags.TOP_NEAR_GRASS))
            Recycle(ref map.last_Pos_Of_Top_Near_Grass, 1.2f, ref map.last_Order_Of_Top_Near_Grass);
        else if (CompareTag(MyTags.TOP_FAR_GRASS))
            Recycle(ref map.last_Pos_Of_Top_Far_Grass, 4.8f, ref map.last_Order_Of_Top_Far_Grass);
        else if (CompareTag(MyTags.BOTTOM_NEAR_GRASS))
            Recycle(ref map.last_Pos_Of_Bottom_Near_Grass, 1.2f, ref map.last_Order_Of_Bottom_Near_Grass);
        else if (CompareTag(MyTags.BOTTOM_FAR_LAND_1))
            Recycle(ref map.last_Pos_Of_Botom_Far_Land_F1, 1.6f, ref map.last_Order_Of_Bottom_Far_Land_F1);
        else if (CompareTag(MyTags.BOTTOM_FAR_LAND_2))
            Recycle(ref map.last_Pos_Of_Botom_Far_Land_F2, 1.6f, ref map.last_Order_Of_Bottom_Far_Land_F2);
        else if (CompareTag(MyTags.BOTTOM_FAR_LAND_3))
            Recycle(ref map.last_Pos_Of_Botom_Far_Land_F3, 1.6f, ref map.last_Order_Of_Bottom_Far_Land_F3);
        else if (CompareTag(MyTags.BOTTOM_FAR_LAND_4))
            Recycle(ref map.last_Pos_Of_Botom_Far_Land_F4, 1.6f, ref map.last_Order_Of_Bottom_Far_Land_F4);
        else if (CompareTag(MyTags.BOTTOM_FAR_LAND_5))
            Recycle(ref map.last_Pos_Of_Botom_Far_Land_F5, 1.6f, ref map.last_Order_Of_Bottom_Far_Land_F5);
    }

    private void Recycle(ref Vector3 nextPosition, float step, ref int nextOrder)
    {
        transform.position = nextPosition;
        nextPosition += Vector3.right * step;
        OffsetSortingOrder(nextOrder - rootRenderer.sortingOrder);
        nextOrder++;
    }

    public void OffsetSortingOrder(int delta)
    {
        if (renderers == null) renderers = GetComponentsInChildren<SpriteRenderer>(true);
        // Decorations must follow their recycled tile, not keep their stale original order.
        for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder += delta;
    }
}
