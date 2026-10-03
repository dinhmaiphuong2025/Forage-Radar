using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ForageRadar;

public class RadarMenu : IClickableMenu
{
    private readonly ModConfig Config;
    private readonly IModHelper Helper;
    private readonly List<ItemRow> Rows = new();
    private int ScrollOffset = 0;
    private const int RowHeight = 68;
    private const int VisibleRowCount = 5;

    public class ItemRow
    {
        public string ItemId { get; }
        public string Name { get; }
        public StardewValley.ItemTypeDefinitions.ParsedItemData? ItemData { get; }
        public bool Enabled { get; set; }

        public ItemRow(string itemId, string name, StardewValley.ItemTypeDefinitions.ParsedItemData? data, bool enabled)
        {
            this.ItemId = itemId;
            this.Name = name;
            this.ItemData = data;
            this.Enabled = enabled;
        }
    }

    public RadarMenu(ModConfig config, IModHelper helper)
        : base(
            x: (Game1.uiViewport.Width - 520) / 2,
            y: (Game1.uiViewport.Height - 480) / 2,
            width: 520,
            height: 480,
            showUpperRightCloseButton: true)
    {
        this.Config = config;
        this.Helper = helper;

        this.PopulateItems();
    }

    private void PopulateItems()
    {
        this.Rows.Clear();
        var seen = new HashSet<string>();

        // 1. Quét mọi item đang có trên map hiện tại
        if (Game1.currentLocation != null)
        {
            foreach (var pair in Game1.currentLocation.Objects.Pairs)
            {
                var obj = pair.Value;
                if (obj == null) continue;
                string id = obj.QualifiedItemId;
                if (!seen.Add(id)) continue;

                if (obj.IsSpawnedObject || obj.Category == StardewValley.Object.GreensCategory || id == "(O)590")
                {
                    var data = ItemRegistry.GetData(id);
                    bool isEnabled = !this.Config.DisabledItemIds.Contains(id);
                    this.Rows.Add(new ItemRow(id, obj.DisplayName, data, isEnabled));
                }
            }
        }

        // 2. Thêm bụi dâu rừng mùa
        string berryId = (Game1.season == Season.Fall) ? "(O)410" : "(O)296";
        if (seen.Add(berryId))
        {
            var data = ItemRegistry.GetData(berryId);
            bool isEnabled = !this.Config.DisabledItemIds.Contains(berryId);
            this.Rows.Add(new ItemRow(berryId, "Bụi dâu rừng chín", data, isEnabled));
        }

        // 3. Nếu danh sách ít quá, thêm các item mùa kinh điển của game để người dùng cấu hình trước
        var seasonalIds = GetCommonSeasonalIds(Game1.season);
        foreach (var id in seasonalIds)
        {
            if (seen.Add(id))
            {
                var data = ItemRegistry.GetData(id);
                if (data != null)
                {
                    bool isEnabled = !this.Config.DisabledItemIds.Contains(id);
                    this.Rows.Add(new ItemRow(id, data.DisplayName, data, isEnabled));
                }
            }
        }
    }

    private static string[] GetCommonSeasonalIds(Season season)
    {
        return season switch
        {
            Season.Spring => new[] { "(O)16", "(O)18", "(O)20", "(O)22", "(O)257", "(O)399" }, // Horseradish, Daffodil, Leek, Dandelion, Morel, Spring Onion
            Season.Summer => new[] { "(O)396", "(O)398", "(O)402", "(O)399" }, // Spice Berry, Grape, Sweet Pea
            Season.Fall => new[] { "(O)404", "(O)406", "(O)408", "(O)410", "(O)281" }, // Common Mushroom, Wild Plum, Hazelnut, Blackberry, Chanterelle
            Season.Winter => new[] { "(O)412", "(O)414", "(O)416", "(O)418", "(O)283" }, // Winter Root, Crystal Fruit, Snow Yam, Crocus, Holly
            _ => Array.Empty<string>()
        };
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        // Click vào các row để toggle Bật / Tắt item
        int startY = this.yPositionOnScreen + 85;
        for (int i = 0; i < VisibleRowCount; i++)
        {
            int index = this.ScrollOffset + i;
            if (index >= this.Rows.Count) break;

            int rowY = startY + i * RowHeight;
            var rowRect = new Rectangle(this.xPositionOnScreen + 30, rowY, this.width - 60, RowHeight - 8);

            if (rowRect.Contains(x, y))
            {
                var row = this.Rows[index];
                row.Enabled = !row.Enabled;

                if (row.Enabled)
                    this.Config.DisabledItemIds.Remove(row.ItemId);
                else
                    this.Config.DisabledItemIds.Add(row.ItemId);

                this.Helper.WriteConfig(this.Config);
                Game1.playSound("drumkit6");
                return;
            }
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        if (direction > 0 && this.ScrollOffset > 0)
            this.ScrollOffset--;
        else if (direction < 0 && this.ScrollOffset + VisibleRowCount < this.Rows.Count)
            this.ScrollOffset++;
    }

    public override void draw(SpriteBatch b)
    {
        // Làm mờ nền phía sau menu
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);

        // Hộp thoại khung viền gỗ chuẩn của Stardew Valley
        Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

        // Tiêu đề
        string title = "Radar Hái Lượm (Bật/Tắt)";
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        b.DrawString(Game1.dialogueFont, title, new Vector2(this.xPositionOnScreen + (this.width - titleSize.X) / 2f, this.yPositionOnScreen + 35), Game1.textColor);

        int startY = this.yPositionOnScreen + 95;
        for (int i = 0; i < VisibleRowCount; i++)
        {
            int index = this.ScrollOffset + i;
            if (index >= this.Rows.Count) break;

            var row = this.Rows[index];
            int rowY = startY + i * RowHeight;
            Rectangle boxRect = new Rectangle(this.xPositionOnScreen + 35, rowY, this.width - 70, RowHeight - 10);

            // Nền nút item
            Color bgColor = row.Enabled ? new Color(180, 230, 180) : new Color(220, 200, 200);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), boxRect.X, boxRect.Y, boxRect.Width, boxRect.Height, bgColor, 4f, false);

            // Icon item
            if (row.ItemData != null)
            {
                Texture2D texture = row.ItemData.GetTexture();
                Rectangle src = row.ItemData.GetSourceRect();
                b.Draw(texture, new Vector2(boxRect.X + 12, boxRect.Y + 8), src, Color.White, 0f, Vector2.Zero, 2.5f, SpriteEffects.None, 0.9f);
            }

            // Tên item
            string displayName = row.Name;
            b.DrawString(Game1.smallFont, displayName, new Vector2(boxRect.X + 65, boxRect.Y + 12), Color.Black);

            // Trạng thái BẬT / TẮT
            string statusText = row.Enabled ? "[ĐANG BẬT]" : "[ĐÃ TẮT]";
            Color statusColor = row.Enabled ? new Color(0, 120, 0) : Color.DarkRed;
            Vector2 statusSize = Game1.smallFont.MeasureString(statusText);
            b.DrawString(Game1.smallFont, statusText, new Vector2(boxRect.Right - statusSize.X - 15, boxRect.Y + 12), statusColor);
        }

        base.draw(b);
        drawMouse(b);
    }
}
