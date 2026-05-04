using UnityEngine;

public interface IArkInteractable
{
    string Prompt { get; }
    void Interact(ArkPlayerController player);
}

public interface IArkSecondaryInteractable
{
    void SecondaryInteract(ArkPlayerController player);
}

public enum BuildingKind
{
    Campfire,
    Stockpile,
    LumberHouse,
    Workshop,
    Dock,
    Ship
}

public static class ArkWorldRules
{
    public const float WaterHeight = -0.72f;
    public const float WorldLimitX = 82f;
    public const float WorldLimitZ = 70f;
    public const float FallRespawnY = -8f;

    public static Vector3 ClampToWorld(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, -WorldLimitX, WorldLimitX);
        position.z = Mathf.Clamp(position.z, -WorldLimitZ, WorldLimitZ);
        return position;
    }

    public static bool IsOutsideWorld(Vector3 position)
    {
        return Mathf.Abs(position.x) > WorldLimitX || Mathf.Abs(position.z) > WorldLimitZ;
    }

    public static bool TryGetGroundY(Vector3 position, out float groundY)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            groundY = position.y;
            return false;
        }

        Vector3 local = position - terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z)
        {
            groundY = position.y;
            return false;
        }

        groundY = terrain.SampleHeight(position) + terrain.transform.position.y;
        return true;
    }

    public static Vector3 ClampToWalkableGround(Vector3 position, float groundOffset)
    {
        position = ClampToWorld(position);
        if (TryGetGroundY(position, out float groundY))
        {
            position.y = Mathf.Max(groundY + groundOffset, WaterHeight + groundOffset);
        }

        return position;
    }
}

public static class ArkSceneMaterials
{
    public static Material Log;
}
