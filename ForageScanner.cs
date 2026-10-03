using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace ForageRadar;

public static class ForageScanner
{
    public static List<TrackedItem> Scan(GameLocation location, Farmer player, ModConfig config)
    {
        var items = new List<TrackedItem>();
        if (location == null || player == null)
            return items;

        Vector2 playerTile = player.Tile;
        Vector2 playerPixel = player.getStandingPosition();
        float maxDistanceTiles = config.ScanRadiusTiles;

        // 1. Quét Objects trên map (GreensCategory, Spawned objects, Artifact Spots)
        foreach (var pair in location.Objects.Pairs)
        {
            Vector2 tile = pair.Key;
            SObject obj = pair.Value;
            if (obj == null)
                continue;

            string id = obj.QualifiedItemId;
            if (config.DisabledItemIds.Contains(id))
                continue;

            float distTiles = Vector2.Distance(playerTile, tile);
            if (distTiles > maxDistanceTiles || distTiles < 0.5f)
                continue;

            bool isForage = false;

            // Kiểm tra item hái lượm tự nhiên (bao gồm theo mùa: hoa, quả, nấm, hạt dại)
            if (obj.IsSpawnedObject)
                isForage = true;
            else if (obj.Category == SObject.GreensCategory)
                isForage = true;
            else if (config.IncludeArtifactSpots && id == "(O)590") // 3 con giun đào đất
                isForage = true;

            if (isForage)
            {
                Vector2 objPixel = new Vector2(tile.X * 64f + 32f, tile.Y * 64f + 32f);
                Vector2 diff = objPixel - playerPixel;
                float angle = (float)Math.Atan2(diff.Y, diff.X);

                var itemData = ItemRegistry.GetData(id);
                items.Add(new TrackedItem(id, obj.DisplayName, objPixel, distTiles, angle, itemData));
            }
        }

        // 2. Quét TerrainFeatures (bụi dâu rừng chín theo mùa)
        foreach (var pair in location.terrainFeatures.Pairs)
        {
            if (pair.Value is Bush bush && bush.tileSheetOffset.Value == 1 && bush.inBloom())
            {
                string berryId = (Game1.season == Season.Fall) ? "(O)410" : "(O)296";
                if (config.DisabledItemIds.Contains(berryId))
                    continue;

                Vector2 tile = pair.Key;
                float distTiles = Vector2.Distance(playerTile, tile);
                if (distTiles <= maxDistanceTiles && distTiles >= 0.5f)
                {
                    Vector2 bushPixel = new Vector2(tile.X * 64f + 32f, tile.Y * 64f + 32f);
                    Vector2 diff = bushPixel - playerPixel;
                    float angle = (float)Math.Atan2(diff.Y, diff.X);

                    var itemData = ItemRegistry.GetData(berryId);
                    items.Add(new TrackedItem(berryId, "Bụi dâu chín", bushPixel, distTiles, angle, itemData));
                }
            }
        }

        // 3. Quét NPC / Dân làng trên bản đồ
        if (config.TrackVillagers)
        {
            foreach (var npc in location.characters)
            {
                if (npc == null || npc.IsMonster || !npc.isVillager())
                    continue;

                string npcId = $"NPC_{npc.Name}";
                if (config.DisabledItemIds.Contains(npcId))
                    continue;

                Vector2 npcPixel = npc.getStandingPosition();
                float distTiles = Vector2.Distance(playerPixel, npcPixel) / 64f;
                if (distTiles > maxDistanceTiles || distTiles < 0.5f)
                    continue;

                Vector2 diff = npcPixel - playerPixel;
                float angle = (float)Math.Atan2(diff.Y, diff.X);

                // Dùng MugShot chân dung chuẩn 16x24 thay vì cắt cụt 16x16
                if (npc.Sprite?.Texture != null)
                {
                    Rectangle mugRect = npc.getMugShotSourceRect();
                    items.Add(new TrackedItem(npcId, npc.displayName, npcPixel, distTiles, angle, npc.Sprite.Texture, mugRect));
                }
            }
        }

        return items;
    }
}
