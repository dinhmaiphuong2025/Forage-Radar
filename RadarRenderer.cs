using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace ForageRadar;

public class RadarRenderer
{
    private readonly ModConfig Config;
    private Texture2D? PixelTexture;

    public RadarRenderer(ModConfig config)
    {
        this.Config = config;
    }

    private Texture2D GetPixel(GraphicsDevice device)
    {
        if (this.PixelTexture == null || this.PixelTexture.IsDisposed)
        {
            this.PixelTexture = new Texture2D(device, 1, 1);
            this.PixelTexture.SetData(new[] { Color.White });
        }
        return this.PixelTexture;
    }

    public void Draw(SpriteBatch b, List<TrackedItem> items, Farmer player)
    {
        if (items.Count == 0 || player == null)
            return;

        // Tọa độ người chơi trên màn hình UI
        Vector2 playerScreenPos = Game1.GlobalToLocal(Game1.viewport, player.getStandingPosition());
        playerScreenPos.Y -= 32f; // Tâm ở ngực nhân vật

        float radius = this.Config.RadarCircleRadius;
        Texture2D pixel = this.GetPixel(b.GraphicsDevice);

        // 1. Vẽ vòng tròn radar mảnh mờ mờ quanh player
        DrawCircle(b, pixel, playerScreenPos, radius, Color.LimeGreen * 0.35f, 32);

        // 2. Vẽ từng item trên vành đai radar
        foreach (var item in items)
        {
            float cos = (float)Math.Cos(item.Angle);
            float sin = (float)Math.Sin(item.Angle);

            Vector2 iconPos = new Vector2(
                playerScreenPos.X + cos * radius,
                playerScreenPos.Y + sin * radius
            );

            // Vẽ mũi tên trỏ ra hướng item
            DrawArrow(b, pixel, iconPos, item.Angle, Color.Yellow);

            // Vẽ icon item từ asset của game
            if (item.ItemData != null)
            {
                Texture2D texture = item.ItemData.GetTexture();
                Rectangle src = item.ItemData.GetSourceRect();

                float scale = this.Config.IconScale;
                Vector2 origin = new Vector2(src.Width / 2f, src.Height / 2f);

                // Viền bóng đen phía sau icon
                b.Draw(texture, iconPos + new Vector2(2, 2), src, Color.Black * 0.6f, 0f, origin, scale, SpriteEffects.None, 0.9f);
                // Icon chính
                b.Draw(texture, iconPos, src, Color.White, 0f, origin, scale, SpriteEffects.None, 1f);
            }

            // Vẽ số khoảng cách (ô tile)
            if (this.Config.ShowDistance)
            {
                string distText = $"{Math.Round(item.DistanceTiles)}m";
                Vector2 textSize = Game1.tinyFont.MeasureString(distText);
                Vector2 textPos = new Vector2(iconPos.X - textSize.X / 2f, iconPos.Y + 16f);

                // Bóng text
                b.DrawString(Game1.tinyFont, distText, textPos + new Vector2(1, 1), Color.Black);
                b.DrawString(Game1.tinyFont, distText, textPos, Color.White);
            }
        }
    }

    private static void DrawCircle(SpriteBatch b, Texture2D pixel, Vector2 center, float radius, Color color, int segments)
    {
        float angleStep = MathHelper.TwoPi / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;

            Vector2 p1 = center + new Vector2((float)Math.Cos(a1), (float)Math.Sin(a1)) * radius;
            Vector2 p2 = center + new Vector2((float)Math.Cos(a2), (float)Math.Sin(a2)) * radius;

            DrawLine(b, pixel, p1, p2, color, 2f);
        }
    }

    private static void DrawArrow(SpriteBatch b, Texture2D pixel, Vector2 pos, float angle, Color color)
    {
        Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        Vector2 arrowTip = pos + dir * 16f;
        DrawLine(b, pixel, pos, arrowTip, color * 0.8f, 3f);
    }

    private static void DrawLine(SpriteBatch b, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 edge = end - start;
        float length = edge.Length();
        if (length < 0.001f)
            return;

        float angle = (float)Math.Atan2(edge.Y, edge.X);
        b.Draw(pixel, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}
