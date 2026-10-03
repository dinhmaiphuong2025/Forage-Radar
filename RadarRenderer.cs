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

    public Rectangle ButtonBounds { get; private set; }

    public RadarRenderer(ModConfig config)
    {
        this.Config = config;
        this.ButtonBounds = new Rectangle(config.ButtonPositionX, config.ButtonPositionY, 60, 60);
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

        // 2. Vẽ vòng tròn radar quanh người chơi trong World
        DrawCircleWorld(b, pixel, playerCenter, radius, Color.LimeGreen * 0.45f, 36);

        // 3. Vẽ từng item trên vành đai radar quanh nhân vật
        foreach (var item in items)
        {
            float cos = (float)Math.Cos(item.Angle);
            float sin = (float)Math.Sin(item.Angle);

            Vector2 iconWorldPos = new Vector2(
                playerCenter.X + cos * radius,
                playerCenter.Y + sin * radius
            );

            // Mũi tên trỏ ra hướng item
            DrawArrowWorld(b, pixel, iconWorldPos, item.Angle, Color.Yellow);

            // Icon item từ game asset
            if (item.ItemData != null)
            {
                Texture2D texture = item.ItemData.GetTexture();
                Rectangle src = item.ItemData.GetSourceRect();

                float scale = this.Config.IconScale;
                Vector2 origin = new Vector2(src.Width / 2f, src.Height / 2f);
                Vector2 drawPos = Game1.GlobalToLocal(Game1.viewport, iconWorldPos);

                // Bóng đổ
                b.Draw(texture, drawPos + new Vector2(2, 2), src, Color.Black * 0.6f, 0f, origin, scale, SpriteEffects.None, 0.9f);
                // Icon chính
                b.Draw(texture, drawPos, src, Color.White, 0f, origin, scale, SpriteEffects.None, 1f);
            }

            // Khoảng cách: Dùng Utility.drawTextWithShadow chuẩn Stardew Valley với smallFont thu nhỏ sắc nét
            if (this.Config.ShowDistance)
            {
                string distText = $"{Math.Round(item.DistanceTiles)}m";
                float textScale = 0.55f; // Tỉ lệ đẹp, sắc nét, không bị vỡ font hay to chữ 'm'
                Vector2 textSize = Game1.smallFont.MeasureString(distText) * textScale;
                Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, iconWorldPos);
                Vector2 textPos = new Vector2(screenPos.X - textSize.X / 2f, screenPos.Y + 16f);

                Utility.drawTextWithShadow(b, distText, Game1.smallFont, textPos, Color.White, textScale);
            }
        }
    }

    // Vẽ nút UI cố định trên màn hình (RenderedHud)
    public void DrawHud(SpriteBatch b)
    {
        Texture2D pixel = this.GetPixel(b.GraphicsDevice);
        this.DrawRadarButton(b, pixel);
    }

    private void DrawRadarButton(SpriteBatch b, Texture2D pixel)
    {
        Rectangle r = this.ButtonBounds;

        // Nền nút
        b.Draw(pixel, r, Color.Black * 0.6f);
        // Viền nút màu vàng kim
        DrawLineScreen(b, pixel, new Vector2(r.Left, r.Top), new Vector2(r.Right, r.Top), Color.Gold, 3f);
        DrawLineScreen(b, pixel, new Vector2(r.Left, r.Bottom), new Vector2(r.Right, r.Bottom), Color.Gold, 3f);
        DrawLineScreen(b, pixel, new Vector2(r.Left, r.Top), new Vector2(r.Left, r.Bottom), Color.Gold, 3f);
        DrawLineScreen(b, pixel, new Vector2(r.Right, r.Top), new Vector2(r.Right, r.Bottom), Color.Gold, 3f);

        // Biểu tượng radar trực quan
        Vector2 center = new Vector2(r.Center.X, r.Center.Y);
        DrawCircleScreen(b, pixel, center, 18f, Color.LimeGreen, 16);
        DrawCircleScreen(b, pixel, center, 8f, Color.LimeGreen * 0.7f, 12);
        b.Draw(pixel, new Rectangle((int)center.X - 3, (int)center.Y - 3, 6, 6), Color.Yellow);
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

    private static void DrawCircleScreen(SpriteBatch b, Texture2D pixel, Vector2 center, float radius, Color color, int segments)
    {
        float angleStep = MathHelper.TwoPi / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;

            Vector2 p1 = center + new Vector2((float)Math.Cos(a1), (float)Math.Sin(a1)) * radius;
            Vector2 p2 = center + new Vector2((float)Math.Cos(a2), (float)Math.Sin(a2)) * radius;

            DrawLineScreen(b, pixel, p1, p2, color, 2f);
        }
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
