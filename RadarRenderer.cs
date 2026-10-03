using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    // Vẽ trong World: Vòng tròn radar luôn neo chính xác quanh người chơi kể cả khi camera dừng ở rìa map/cổng map
    public void DrawWorld(SpriteBatch b, List<TrackedItem> items, Farmer player)
    {
        if (items.Count == 0 || player == null)
            return;

        Texture2D pixel = this.GetPixel(b.GraphicsDevice);

        // 1. Tọa độ tâm người chơi trong Map World (chính giữa bounding box)
        Rectangle bb = player.GetBoundingBox();
        Vector2 playerCenter = new Vector2(bb.Center.X, bb.Center.Y - 16f);

        float radius = this.Config.RadarCircleRadius;

        // 2. Tùy chọn Bật/Tắt vẽ vòng tròn radar quanh người chơi
        if (this.Config.ShowCircle)
        {
            DrawCircleWorld(b, pixel, playerCenter, radius, Color.LimeGreen * 0.45f, 36);
        }

        // 3. Vẽ từng item trên vành đai radar quanh nhân vật
        foreach (var item in items)
        {
            float cos = (float)Math.Cos(item.Angle);
            float sin = (float)Math.Sin(item.Angle);

            Vector2 iconWorldPos = new Vector2(
                playerCenter.X + cos * radius,
                playerCenter.Y + sin * radius
            );

            // Mũi tên trỏ ra hướng item / NPC
            Color arrowColor = item.IsNpc ? Color.Cyan : Color.Yellow;
            DrawArrowWorld(b, pixel, iconWorldPos, item.Angle, arrowColor);

            Vector2 drawPos = Game1.GlobalToLocal(Game1.viewport, iconWorldPos);

            // Icon item từ game asset hoặc Sprite NPC
            if (item.IsNpc && item.CustomTexture != null)
            {
                float scale = this.Config.IconScale * 1.5f;
                Rectangle src = item.CustomSourceRect;
                Vector2 origin = new Vector2(src.Width / 2f, src.Height / 2f);

                // Bóng đổ
                b.Draw(item.CustomTexture, drawPos + new Vector2(2, 2), src, Color.Black * 0.6f, 0f, origin, scale, SpriteEffects.None, 0.9f);
                // Head Sprite chính
                b.Draw(item.CustomTexture, drawPos, src, Color.White, 0f, origin, scale, SpriteEffects.None, 1f);
            }
            else if (item.ItemData != null)
            {
                Texture2D texture = item.ItemData.GetTexture();
                Rectangle src = item.ItemData.GetSourceRect();

                float scale = this.Config.IconScale;
                Vector2 origin = new Vector2(src.Width / 2f, src.Height / 2f);

                // Bóng đổ
                b.Draw(texture, drawPos + new Vector2(2, 2), src, Color.Black * 0.6f, 0f, origin, scale, SpriteEffects.None, 0.9f);
                // Icon chính
                b.Draw(texture, drawPos, src, Color.White, 0f, origin, scale, SpriteEffects.None, 1f);
            }

            // Khoảng cách: Hiển thị font chữ sắc nét và hỗ trợ tuỳ chỉnh kích thước font qua DistanceFontScale
            if (this.Config.ShowDistance)
            {
                string distText = $"{Math.Round(item.DistanceTiles)}m";
                float textScale = this.Config.DistanceFontScale;
                Vector2 textSize = Game1.smallFont.MeasureString(distText) * textScale;
                Vector2 textPos = new Vector2(drawPos.X - textSize.X / 2f, drawPos.Y + 16f);

                Utility.drawTextWithShadow(b, distText, Game1.smallFont, textPos, Color.White, textScale);
            }
        }
    }

    private static void DrawCircleWorld(SpriteBatch b, Texture2D pixel, Vector2 centerWorld, float radius, Color color, int segments)
    {
        float angleStep = MathHelper.TwoPi / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;

            Vector2 p1World = centerWorld + new Vector2((float)Math.Cos(a1), (float)Math.Sin(a1)) * radius;
            Vector2 p2World = centerWorld + new Vector2((float)Math.Cos(a2), (float)Math.Sin(a2)) * radius;

            Vector2 p1Screen = Game1.GlobalToLocal(Game1.viewport, p1World);
            Vector2 p2Screen = Game1.GlobalToLocal(Game1.viewport, p2World);

            DrawLineScreen(b, pixel, p1Screen, p2Screen, color, 2f);
        }
    }

    private static void DrawArrowWorld(SpriteBatch b, Texture2D pixel, Vector2 posWorld, float angle, Color color)
    {
        Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        Vector2 tipWorld = posWorld + dir * 14f;

        Vector2 p1 = Game1.GlobalToLocal(Game1.viewport, posWorld);
        Vector2 p2 = Game1.GlobalToLocal(Game1.viewport, tipWorld);

        DrawLineScreen(b, pixel, p1, p2, color * 0.85f, 3f);
    }

    private static void DrawLineScreen(SpriteBatch b, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 edge = end - start;
        float length = edge.Length();
        if (length < 0.001f)
            return;

        float angle = (float)Math.Atan2(edge.Y, edge.X);
        b.Draw(pixel, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}
