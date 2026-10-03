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
    private readonly List<ItemRow> AllRows = new();
    private readonly List<ItemRow> FilteredRows = new();

    private TextBox SearchBox;
    private ClickableComponent SearchBoxCC;

    // Parallax scrolling
    private float CurrentScrollY = 0f;
    private float TargetScrollY = 0f;
    private bool IsDragging = false;
    private int DragStartY = 0;
    private float DragStartScrollY = 0f;

    private const int RowHeight = 72;
    private const int ViewportHeight = 360;

    public class ItemRow
    {
        public string Id { get; }
        public string Name { get; }
        public string Category { get; } // "Item" hoặc "Dân làng"
        public bool IsNpc { get; }
        public Texture2D? Texture { get; }
        public Rectangle SourceRect { get; }
        public bool Enabled { get; set; }

        public ItemRow(string id, string name, string category, bool isNpc, Texture2D? texture, Rectangle sourceRect, bool enabled)
        {
            this.Id = id;
            this.Name = name;
            this.Category = category;
            this.IsNpc = isNpc;
            this.Texture = texture;
            this.SourceRect = sourceRect;
            this.Enabled = enabled;
        }
    }

    public RadarMenu(ModConfig config, IModHelper helper)
        : base(
            x: (Game1.uiViewport.Width - 620) / 2,
            y: (Game1.uiViewport.Height - 560) / 2,
            width: 620,
            height: 560,
            showUpperRightCloseButton: true)
    {
        this.Config = config;
        this.Helper = helper;

        // Ô tìm kiếm bằng chữ chuẩn Stardew Valley
        Texture2D textBoxTex = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
        this.SearchBox = new TextBox(textBoxTex, null, Game1.smallFont, Color.Black)
        {
            X = this.xPositionOnScreen + 60,
            Y = this.yPositionOnScreen + 85,
            Width = this.width - 120,
            Height = 48
        };
        this.SearchBoxCC = new ClickableComponent(new Rectangle(this.SearchBox.X, this.SearchBox.Y, this.SearchBox.Width, this.SearchBox.Height), "");

        this.PopulateAll();
        this.ApplySearchFilter();
    }

    private void PopulateAll()
    {
        this.AllRows.Clear();
        var seen = new HashSet<string>();

        // 1. Quét dân làng / NPC trước
        if (Game1.currentLocation != null)
        {
            foreach (var npc in Game1.currentLocation.characters)
            {
                if (npc == null || npc.IsMonster || !npc.isVillager())
                    continue;

                string npcId = $"NPC_{npc.Name}";
                if (seen.Add(npcId) && npc.Sprite?.Texture != null)
                {
                    bool isEnabled = !this.Config.DisabledItemIds.Contains(npcId);
                    this.AllRows.Add(new ItemRow(npcId, npc.displayName, "Dân làng", true, npc.Sprite.Texture, new Rectangle(0, 0, 16, 16), isEnabled));
                }
            }
        }

        // Thêm danh sách một số dân làng quen thuộc nếu map hiện tại vắng vẻ
        string[] commonNpcs = { "Robin", "Demetrius", "Maru", "Sebastian", "Linus", "Pierre", "Caroline", "Abigail", "Alex", "George", "Evelyn", "Lewis", "Clint", "Penny", "Pam", "Emily", "Haley", "Jas", "Vincent", "Jodi", "Kent", "Sam", "Leah", "Marnie", "Shane", "Harvey", "Elliott", "Gus", "Willy", "Wizard" };
        foreach (var name in commonNpcs)
        {
            string id = $"NPC_{name}";
            if (seen.Add(id))
            {
                var charData = Game1.getCharacterFromName(name);
                if (charData?.Sprite?.Texture != null)
                {
                    bool isEnabled = !this.Config.DisabledItemIds.Contains(id);
                    this.AllRows.Add(new ItemRow(id, charData.displayName, "Dân làng", true, charData.Sprite.Texture, new Rectangle(0, 0, 16, 16), isEnabled));
                }
            }
        }

        // 2. Điểm đào kho báu 3 con giun (Artifact Spot)
        if (seen.Add("(O)590"))
        {
            var data = ItemRegistry.GetData("(O)590");
            bool isEnabled = !this.Config.DisabledItemIds.Contains("(O)590");
            this.AllRows.Add(new ItemRow("(O)590", "Điểm đào giun (Cổ vật / Kho báu)", "Kho báu", false, data?.GetTexture(), data != null ? data.GetSourceRect() : Rectangle.Empty, isEnabled));
        }

        // 3. Quét mọi item đang có trên map
        if (Game1.currentLocation != null)
        {
            foreach (var pair in Game1.currentLocation.Objects.Pairs)
            {
                var obj = pair.Value;
                if (obj == null) continue;
                string id = obj.QualifiedItemId;
                if (!seen.Add(id)) continue;

                if (obj.IsSpawnedObject || obj.Category == StardewValley.Object.GreensCategory)
                {
                    var data = ItemRegistry.GetData(id);
                    bool isEnabled = !this.Config.DisabledItemIds.Contains(id);
                    this.AllRows.Add(new ItemRow(id, obj.DisplayName, "Hái lượm", false, data?.GetTexture(), data != null ? data.GetSourceRect() : Rectangle.Empty, isEnabled));
                }
            }
        }

        // 4. Bụi dâu rừng chín theo mùa
        string berryId = (Game1.season == Season.Fall) ? "(O)410" : "(O)296";
        if (seen.Add(berryId))
        {
            var data = ItemRegistry.GetData(berryId);
            bool isEnabled = !this.Config.DisabledItemIds.Contains(berryId);
            this.AllRows.Add(new ItemRow(berryId, "Bụi dâu rừng chín", "Mùa vụ", false, data?.GetTexture(), data != null ? data.GetSourceRect() : Rectangle.Empty, isEnabled));
        }

        // 5. Item các mùa trong năm
        string[] allSeasonal = {
            "(O)16", "(O)18", "(O)20", "(O)22", "(O)257", "(O)399", // Xuân
            "(O)396", "(O)398", "(O)402", // Hạ
            "(O)404", "(O)406", "(O)408", "(O)281", // Thu
            "(O)412", "(O)414", "(O)416", "(O)418", "(O)283" // Đông
        };
        foreach (var id in allSeasonal)
        {
            if (seen.Add(id))
            {
                var data = ItemRegistry.GetData(id);
                if (data != null)
                {
                    bool isEnabled = !this.Config.DisabledItemIds.Contains(id);
                    this.AllRows.Add(new ItemRow(id, data.DisplayName, "Hái lượm", false, data.GetTexture(), data.GetSourceRect(), isEnabled));
                }
            }
        }
    }

    private void ApplySearchFilter()
    {
        string query = this.SearchBox.Text?.Trim().ToLower() ?? "";
        this.FilteredRows.Clear();

        foreach (var row in this.AllRows)
        {
            if (string.IsNullOrEmpty(query) || row.Name.ToLower().Contains(query) || row.Category.ToLower().Contains(query))
            {
                this.FilteredRows.Add(row);
            }
        }

        this.ClampScroll();
    }

    private float GetMaxScroll()
    {
        float totalHeight = this.FilteredRows.Count * RowHeight;
        return Math.Max(0f, totalHeight - ViewportHeight);
    }

    private void ClampScroll()
    {
        float max = this.GetMaxScroll();
        this.TargetScrollY = MathHelper.Clamp(this.TargetScrollY, 0f, max);
    }

    public override void update(GameTime time)
    {
        base.update(time);

        // Hiệu ứng Parallax Smooth Scrolling (nội suy mượt mà)
        if (Math.Abs(this.CurrentScrollY - this.TargetScrollY) > 0.1f)
        {
            this.CurrentScrollY = MathHelper.Lerp(this.CurrentScrollY, this.TargetScrollY, 0.22f);
        }
        else
        {
            this.CurrentScrollY = this.TargetScrollY;
        }

        // Tự động cập nhật bộ lọc nếu người chơi gõ chữ
        this.SearchBox.Update();
    }

    public override void receiveKeyPress(Microsoft.Xna.Framework.Input.Keys key)
    {
        if (this.SearchBox.Selected)
        {
            this.ApplySearchFilter();
            return;
        }

        base.receiveKeyPress(key);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        this.TargetScrollY -= (direction > 0 ? 1f : -1f) * RowHeight * 1.5f;
        this.ClampScroll();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        // Chạm vào ô tìm kiếm
        if (this.SearchBoxCC.containsPoint(x, y))
        {
            this.SearchBox.SelectMe();
            return;
        }
        this.SearchBox.Selected = false;

        // Vùng hiển thị danh sách (viewport)
        int listTop = this.yPositionOnScreen + 145;
        int listBottom = listTop + ViewportHeight;
        Rectangle listRect = new Rectangle(this.xPositionOnScreen + 30, listTop, this.width - 60, ViewportHeight);

        if (listRect.Contains(x, y))
        {
            this.IsDragging = true;
            this.DragStartY = y;
            this.DragStartScrollY = this.TargetScrollY;

            // Kiểm tra click vào row
            float relativeY = (y - listTop) + this.CurrentScrollY;
            int clickedIndex = (int)(relativeY / RowHeight);

            if (clickedIndex >= 0 && clickedIndex < this.FilteredRows.Count)
            {
                var row = this.FilteredRows[clickedIndex];
                row.Enabled = !row.Enabled;

                if (row.Enabled)
                    this.Config.DisabledItemIds.Remove(row.Id);
                else
                    this.Config.DisabledItemIds.Add(row.Id);

                this.Helper.WriteConfig(this.Config);
                Game1.playSound("drumkit6");
            }
        }
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);

        // Kéo vuốt cảm ứng trên màn hình điện thoại (Touch Drag)
        if (this.IsDragging)
        {
            int deltaY = this.DragStartY - y;
            this.TargetScrollY = this.DragStartScrollY + deltaY;
            this.ClampScroll();
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        this.IsDragging = false;
    }

    public override void draw(SpriteBatch b)
    {
        // 1. Phông nền làm mờ
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);

        // 2. Khung viền gỗ hộp thoại chính của Stardew Valley
        Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

        // 3. Tiêu đề Menu
        string title = "Radar Hái Lượm & Dân Làng";
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        b.DrawString(Game1.dialogueFont, title, new Vector2(this.xPositionOnScreen + (this.width - titleSize.X) / 2f, this.yPositionOnScreen + 35), Game1.textColor);

        // 4. Ô gõ tìm kiếm (Search Box)
        this.SearchBox.Draw(b);
        if (string.IsNullOrEmpty(this.SearchBox.Text) && !this.SearchBox.Selected)
        {
            b.DrawString(Game1.smallFont, "Nhập tên item hoặc dân làng để tìm kiếm...", new Vector2(this.SearchBox.X + 16, this.SearchBox.Y + 12), Color.Gray);
        }

        // 5. Vùng Parallax Scrolling List với ScissorRectangle
        int listTop = this.yPositionOnScreen + 145;
        Rectangle listRect = new Rectangle(this.xPositionOnScreen + 35, listTop, this.width - 70, ViewportHeight);

        // Lớp nền nền phụ cho danh sách tạo cảm giác chiều sâu (Parallax layer 1: Tốc độ 0.3x)
        float bgParallaxOffset = (this.CurrentScrollY * 0.25f) % 32f;
        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(64, 320, 64, 64), listRect.X, listRect.Y, listRect.Width, listRect.Height, Color.Wheat * 0.35f, 2f, false);

        // Cắt góc Scissor để cuộn không tràn ra ngoài viền
        RasterizerState prevRaster = b.GraphicsDevice.RasterizerState;
        Rectangle prevScissor = b.GraphicsDevice.ScissorRectangle;

        b.End();
        RasterizerState rState = new RasterizerState { ScissorTestEnable = true };
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rState);
        b.GraphicsDevice.ScissorRectangle = listRect;

        // Vẽ các dòng item (Parallax layer 2: Tốc độ cuộn 1.0x)
        for (int i = 0; i < this.FilteredRows.Count; i++)
        {
            float rowY = listTop + (i * RowHeight) - this.CurrentScrollY;

            // Bỏ qua vẽ các phần tử nằm ngoài tầm nhìn để tối ưu hiệu năng
            if (rowY + RowHeight < listTop || rowY > listTop + ViewportHeight)
                continue;

            var row = this.FilteredRows[i];
            Rectangle boxRect = new Rectangle(listRect.X + 4, (int)rowY + 2, listRect.Width - 8, RowHeight - 6);

            // Nền nút item / NPC
            Color bgColor = row.Enabled ? (row.IsNpc ? new Color(200, 230, 255) : new Color(190, 240, 190)) : new Color(230, 210, 210);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), boxRect.X, boxRect.Y, boxRect.Width, boxRect.Height, bgColor, 4f, false);

            // Icon Sprite (Item hoặc Avatar Dân làng)
            if (row.Texture != null)
            {
                float iconScale = row.IsNpc ? 2.8f : 2.5f;
                b.Draw(row.Texture, new Vector2(boxRect.X + 16, boxRect.Y + 10), row.SourceRect, Color.White, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0.9f);
            }

            // Tên item / NPC
            b.DrawString(Game1.smallFont, row.Name, new Vector2(boxRect.X + 76, boxRect.Y + 12), Color.Black);

            // Tag phân loại dạng Pill gọn gàng, tinh tế, không vỡ layout
            string categoryTag = row.Category;
            Vector2 catSize = Game1.tinyFont.MeasureString(categoryTag);
            Rectangle pillRect = new Rectangle(boxRect.X + 76, boxRect.Y + 40, (int)catSize.X + 16, 20);
            b.Draw(Game1.fadeToBlackRect, pillRect, Color.Black * 0.15f);
            b.DrawString(Game1.tinyFont, categoryTag, new Vector2(pillRect.X + 8, pillRect.Y + 1), Color.DarkSlateGray);

            // Nhãn trạng thái BẬT / TẮT
            string statusText = row.Enabled ? "BẬT" : "TẮT";
            Color statusColor = row.Enabled ? new Color(0, 130, 0) : Color.DarkRed;
            Vector2 statusSize = Game1.smallFont.MeasureString(statusText);
            b.DrawString(Game1.smallFont, statusText, new Vector2(boxRect.Right - statusSize.X - 24, boxRect.Y + 18), statusColor);
        }

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, prevRaster);
        b.GraphicsDevice.ScissorRectangle = prevScissor;

        // Vẽ thanh cuộn ScrollBar thanh lịch
        float maxScroll = this.GetMaxScroll();
        if (maxScroll > 0)
        {
            int scrollBarX = listRect.Right - 8;
            int barHeight = ViewportHeight;
            float thumbHeight = Math.Max(30f, (float)ViewportHeight / (this.FilteredRows.Count * RowHeight) * ViewportHeight);
            float thumbY = listTop + (this.CurrentScrollY / maxScroll) * (ViewportHeight - thumbHeight);

            b.Draw(Game1.fadeToBlackRect, new Rectangle(scrollBarX, listTop, 6, barHeight), Color.Black * 0.2f);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(scrollBarX, (int)thumbY, 6, (int)thumbHeight), Color.Goldenrod * 0.8f);
        }

        base.draw(b);
        drawMouse(b);
    }
}
