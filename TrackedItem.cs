using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace ForageRadar;

public class TrackedItem
{
    public string DisplayName { get; }
    public Vector2 WorldPosition { get; }
    public float DistanceTiles { get; }
    public float Angle { get; }
    public ParsedItemData? ItemData { get; }

    public TrackedItem(string displayName, Vector2 worldPosition, float distanceTiles, float angle, ParsedItemData? itemData)
    {
        this.DisplayName = displayName;
        this.WorldPosition = worldPosition;
        this.DistanceTiles = distanceTiles;
        this.Angle = angle;
        this.ItemData = itemData;
    }
}
