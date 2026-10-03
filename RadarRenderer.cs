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
        this.ButtonBounds = new Rectangle(config.ButtonPositionX, config.ButtonPositionY, 52, 52);
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
        Texture2D pixel = this.GetPixel(b.GraphicsDevice);

        // 1. Vẽ nút radar nổi trên màn hình để mở menu
        this.DrawRadarButton(b, pixel);

        if (items.Count == 0 || player == null)
            return;

        // 2. Tọa độ chính giữa bounding box nhân vật (neo chuẩn xác tâm người chơi)
        Rectangle playerBoundingBox = player.GetBoundingBox();
        Vector2 playerCenterWorld = new Vector2(
            playerBoundingBox.Center.X,
            playerBoundingBox.Center.Y - 16f
        );

        // Chuyển sang tọa độ viewport màn hình
        Vector2 playerScreenPos = new Vector2(
            playerCenterWorld.X - Game1.viewport.X,
            playerCenterWorld.Y - Game1.viewport.Y
        );

        float radius = this.Config.RadarCircleRadius;

        // 3. Vẽ vòng tròn radar mảnh quanh player
        DrawCircle(b, pixel, playerScreenPos, radius, Color.LimeGreen * 0.4f, 32);

        // 4. Vẽ từng item trên vành đai radar
        foreach (var item in items)
        {
            float cos = (float)Math.Cos(item.Angle);
            float sin = (float)Math.Sin(item.Angle);

            Vector2 iconPos = new Vector2(
                playerScreenPos.X + cos * radius,
                playerScreenPos.Y + sin * radius
            );

            // Mũi tên trỏ ra hướng item
            DrawArrow(b, pixel, iconPos, item.Angle, Color.Yellow);

            // Icon item từ game asset
            if (item.ItemData != null)
            {
                Texture2D texture = item.ItemData.GetTexture();
                Rectangle src = item.ItemData.GetSourceRect();

                float scale = this.Config.IconScale;
                Vector2 origin = new Vector2(src.Width / 2f, src.Height / 2f);

                // Bóng đen
                b.Draw(texture, iconPos + new Vector2(2, 2), src, Color.Black * 0.6f, 0f, origin, scale, SpriteEffects.None, 0.9f);
                // Icon chính
                b.Draw(texture, iconPos, src, Color.White, 0f, origin, scale, SpriteEffects.None, 1f);
            }

            // Khoảng cách: Dùng tinyFont đồng nhất với scale nhỏ gọn
            if (this.Config.ShowDistance)
            {
                string distText = $"{Math.Round(item.DistanceTiles)}m";
                float textScale = 0.85f; // Thu nhỏ chữ để vừa vặn, không bị to đột biến
                Vector2 textSize = Game1.tinyFont.MeasureString(distText) * textScale;
                Vector2 textPos = new Vector2(iconPos.X - textSize.X / 2f, iconPos.Y + 16f);

                // Bóng đen
                b.DrawString(Game1.tinyFont, distText, textPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, textScale, SpriteEffects.None, 1f);
                // Chữ chính màu trắng
                b.DrawString(Game1.tinyFont, distText, textPos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 1f);
            }
        }
    }

    private void DrawRadarButton(SpriteBatch b, Texture2D pixel)
    {
        Rectangle r = this.ButtonBounds;

        // Nền nút
        b.Draw(pixel, r, Color.Black * 0.5f);
        // Viền nút
        DrawLine(b, pixel, new Vector2(r.Left, r.Top), new Vector2(r.Right, r.Top), Color.Gold, 2f);
        DrawLine(b, pixel, new Vector2(r.Left, r.Bottom), new Vector2(r.Right, r.Bottom), Color.Gold, 2f);
        DrawLine(b, pixel, new Vector2(r.Left, r.Top), new Vector2(r.Left, r.Bottom), Color.Gold, 2f);
        DrawLine(b, pixel, new Vector2(r.Right, r.Top), new Vector2(r.Right, r.Bottom), Color.Gold, 2f);

        // Vẽ biểu tượng radar nhỏ ở giữa nút
        Vector2 center = new Vector2(r.Center.X, r.Center.Y);
        DrawCircle(b, pixel, center, 14f, Color.LimeGreen, 16);
        DrawCircle(b, pixel, center, 6f, Color.LimeGreen * 0.7f, 12);
        b.Draw(pixel, new Rectangle((int)center.X - 2, (int)center.Y - 2, 4, 4), Color.Yellow);
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
        Vector2 arrowTip = pos + dir * 14f;
        DrawLine(b, pixel, pos, arrowTip, color * 0.85f, 3f);
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
