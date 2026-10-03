using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace ForageRadar;

public class TrackedItem
{
    public string ItemId { get; }
    public string DisplayName { get; }
    public Vector2 WorldPosition { get; }
    public float DistanceTiles { get; }
    public float Angle { get; }
    public ParsedItemData? ItemData { get; }

    // Dành riêng cho NPC / Dân làng
    public bool IsNpc { get; }
    public Texture2D? CustomTexture { get; }
    public Rectangle CustomSourceRect { get; }

    public TrackedItem(string itemId, string displayName, Vector2 worldPosition, float distanceTiles, float angle, ParsedItemData? itemData)
    {
        this.ItemId = itemId;
        this.DisplayName = displayName;
        this.WorldPosition = worldPosition;
        this.DistanceTiles = distanceTiles;
        this.Angle = angle;
        this.ItemData = itemData;
        this.IsNpc = false;
        this.CustomTexture = null;
        this.CustomSourceRect = Rectangle.Empty;
    }

    public TrackedItem(string id, string name, Vector2 worldPos, float dist, float angle, Texture2D texture, Rectangle srcRect)
    {
        this.ItemId = id;
        this.DisplayName = name;
        this.WorldPosition = worldPos;
        this.DistanceTiles = dist;
        this.Angle = angle;
        this.ItemData = null;
        this.IsNpc = true;
        this.CustomTexture = texture;
        this.CustomSourceRect = srcRect;
    }
}
