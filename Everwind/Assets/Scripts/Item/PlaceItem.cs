using UnityEngine;

[CreateAssetMenu]
public class PlaceItem : ConsumeItem
{
    public GameObject Placement;

    public override void Use()
    {
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj == null)
            return;

        Player player = playerObj.GetComponent<Player>();
        if (player == null)
            return;

        Vector3 groundPos = player.GetPlacePositionForItem();

        ExecutePlacement(groundPos, player.transform.rotation);
    }

    private void ExecutePlacement(Vector3 groundPos, Quaternion rotation)
    {
        if (Placement == null)
            return;

        GameObject spawnedObj = Instantiate(Placement, groundPos, rotation);

        MeshRenderer[] renderers = spawnedObj.GetComponentsInChildren<MeshRenderer>();

        if (renderers.Length > 0)
        {
            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                totalBounds.Encapsulate(renderers[i].bounds);
            }

            float bottomY = totalBounds.min.y;
            float offsetY = spawnedObj.transform.position.y - bottomY;

            spawnedObj.transform.position += new Vector3(0, offsetY, 0);
        }
    }
}
