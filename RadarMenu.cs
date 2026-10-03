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

    // 2 Tabs: 0 = Dân Làng, 1 = Vật Phẩm & Kho Báu
    private int ActiveTab = 0;
    private Rectangle TabVillagersRect;
    private Rectangle TabItemsRect;

    // Parallax scrolling & Touch Gestures
    private float CurrentScrollY = 0f;
    private float TargetScrollY = 0f;
    private bool IsDragging = false;
    private bool IsDraggingScrollBar = false;
    private int TouchStartX = 0;
    private int TouchStartY = 0;
    private float DragStartScrollY = 0f;
    private bool HasMovedEnough = false;
    private int PotentialClickedIndex = -1;

    private const int RowHeight = 84;
    private const int ViewportHeight = 405;
    private const int TouchSlop = 14;

    public class ItemRow
    {
        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
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

    public RadarMenu(ModConfig config, IModHelper helper, int initialTab = 0)
        : base(
            x: (Game1.uiViewport.Width - 680) / 2,
            y: (Game1.uiViewport.Height - 600) / 2,
            width: 680,
            height: 600,
            showUpperRightCloseButton: true)
    {
        this.Config = config;
        this.Helper = helper;
        this.ActiveTab = initialTab;

        // Tọa độ các thành phần bên trong menu (padding 44px mỗi bên để gọn gàng hoàn toàn trong viền hộp thoại)
        int innerX = this.xPositionOnScreen + 44;
        int innerWidth = this.width - 88;

        // Vị trí 2 Tabs
        int tabWidth = (innerWidth - 14) / 2;
        int tabY = this.yPositionOnScreen + 82;
        this.TabVillagersRect = new Rectangle(innerX, tabY, tabWidth, 46);
        this.TabItemsRect = new Rectangle(innerX + tabWidth + 14, tabY, tabWidth, 46);

        this.PopulateAll();
        this.ApplyFilter();
    }

    private void PopulateAll()
    {
        this.AllRows.Clear();
        var seen = new HashSet<string>();

        // 1. NPC / Dân làng trong map hiện tại
        if (Game1.currentLocation != null)
        {
            foreach (var npc in Game1.currentLocation.characters)
            {
                if (npc == null || npc.IsMonster || !npc.IsVillager)
                    continue;

                string npcId = $"NPC_{npc.Name}";
                if (seen.Add(npcId) && npc.Sprite?.Texture != null)
                {
                    bool isEnabled = !this.Config.DisabledItemIds.Contains(npcId);
                    Rectangle mugRect = npc.getMugShotSourceRect();
                    this.AllRows.Add(new ItemRow(npcId, npc.displayName, "Dân làng", true, npc.Sprite.Texture, mugRect, isEnabled));
                }
            }
        }

        // Danh sách NPC quen thuộc nếu map hiện tại ít người
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
                    Rectangle mugRect = charData.getMugShotSourceRect();
                    this.AllRows.Add(new ItemRow(id, charData.displayName, "Dân làng", true, charData.Sprite.Texture, mugRect, isEnabled));
                }
            }
        }

        // 2. Điểm đào kho báu 3 con giun (Artifact Spot)
        if (seen.Add("(O)590"))
        {
            var data = ItemRegistry.GetData("(O)590");
            bool isEnabled = !this.Config.DisabledItemIds.Contains("(O)590");
            this.AllRows.Add(new ItemRow("(O)590", "Điểm đào giun (Kho báu)", "Kho báu", false, data?.GetTexture(), data != null ? data.GetSourceRect() : Rectangle.Empty, isEnabled));
        }

        // 3. Mọi item hái lượm đang có trên map
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
                    this.AllRows.Add(new ItemRow(id, obj.DisplayName, "Thu lượm", false, data?.GetTexture(), data != null ? data.GetSourceRect() : Rectangle.Empty, isEnabled));
                }
            }
        }

        // 4. Bụi dâu rừng chín theo mùa
        string berryId = (Game1.season == Season.Fall) ? "(O)410" : "(O)296";
        if (seen.Add(berryId))
        {
            var data = ItemRegistry.GetData(berryId);
            bool isEnabled = !this.Config.DisabledItemIds.Contains(berryId);
            this.AllRows.Add(new ItemRow(berryId, "Bụi dâu chín", "Mùa vụ", false, data?.GetTexture(), data != null ? data.GetSourceRect() : Rectangle.Empty, isEnabled));
        }

        // 5. Item đặc trưng các mùa
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
                    this.AllRows.Add(new ItemRow(id, data.DisplayName, "Thu lượm", false, data.GetTexture(), data.GetSourceRect(), isEnabled));
                }
            }
        }
    }

    private void ApplyFilter()
    {
        this.FilteredRows.Clear();

        foreach (var row in this.AllRows)
        {
            // Lọc theo Tab: 0 = Dân Làng, 1 = Vật Phẩm / Kho Báu
            if (this.ActiveTab == 0 && !row.IsNpc)
                continue;
            if (this.ActiveTab == 1 && row.IsNpc)
                continue;

            this.FilteredRows.Add(row);
        }

        this.TargetScrollY = 0f;
        this.CurrentScrollY = 0f;
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

        // Nội suy mượt mà Parallax Scrolling
        if (Math.Abs(this.CurrentScrollY - this.TargetScrollY) > 0.1f)
        {
            this.CurrentScrollY = MathHelper.Lerp(this.CurrentScrollY, this.TargetScrollY, 0.25f);
        }
        else
        {
            this.CurrentScrollY = this.TargetScrollY;
        }
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

        // 1. Chuyển Tab: Tab 1 Dân Làng
        if (this.TabVillagersRect.Contains(x, y))
        {
            if (this.ActiveTab != 0)
            {
                this.ActiveTab = 0;
                Game1.playSound("shwip");
                this.ApplyFilter();
            }
            return;
        }

        // Tab 2 Vật Phẩm
        if (this.TabItemsRect.Contains(x, y))
        {
            if (this.ActiveTab != 1)
            {
                this.ActiveTab = 1;
                Game1.playSound("shwip");
                this.ApplyFilter();
            }
            return;
        }

        // 2. Vùng danh sách và thanh cuộn
        int listTop = this.yPositionOnScreen + 140;
        int listWidth = this.width - 88;
        Rectangle listRect = new Rectangle(this.xPositionOnScreen + 44, listTop, listWidth, ViewportHeight);
        Rectangle scrollBarArea = new Rectangle(listRect.Right - 28, listTop, 32, ViewportHeight);

        // Kiểm tra thanh cuộn ScrollBar
        if (scrollBarArea.Contains(x, y))
        {
            this.IsDraggingScrollBar = true;
            this.UpdateScrollFromTouchY(y, listTop);
            return;
        }

        // Kiểm tra danh sách item
        if (listRect.Contains(x, y))
        {
            this.IsDragging = true;
            this.TouchStartX = x;
            this.TouchStartY = y;
            this.DragStartScrollY = this.TargetScrollY;
            this.HasMovedEnough = false;

            float relativeY = (y - listTop) + this.CurrentScrollY;
            this.PotentialClickedIndex = (int)(relativeY / RowHeight);
        }
    }

    private void UpdateScrollFromTouchY(int y, int listTop)
    {
        float maxScroll = this.GetMaxScroll();
        if (maxScroll <= 0) return;

        float relativeY = MathHelper.Clamp(y - listTop, 0, ViewportHeight);
        this.TargetScrollY = (relativeY / ViewportHeight) * maxScroll;
        this.ClampScroll();
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);

        int listTop = this.yPositionOnScreen + 140;

        if (this.IsDraggingScrollBar)
        {
            this.UpdateScrollFromTouchY(y, listTop);
            return;
        }

        if (this.IsDragging)
        {
            int deltaY = y - this.TouchStartY;
            int deltaX = x - this.TouchStartX;

            if (Math.Abs(deltaY) > TouchSlop || Math.Abs(deltaX) > TouchSlop)
            {
                this.HasMovedEnough = true;
            }

            if (this.HasMovedEnough)
            {
                this.TargetScrollY = this.DragStartScrollY - deltaY;
                this.ClampScroll();
            }
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);

        // Chỉ toggle BẬT / TẮT khi người dùng chạm và nhấc ngón tay lên, không thực hiện thao tác vuốt cuộn
        if (this.IsDragging && !this.HasMovedEnough && this.PotentialClickedIndex >= 0 && this.PotentialClickedIndex < this.FilteredRows.Count)
        {
            var row = this.FilteredRows[this.PotentialClickedIndex];
            row.Enabled = !row.Enabled;

            if (row.Enabled)
                this.Config.DisabledItemIds.Remove(row.Id);
            else
                this.Config.DisabledItemIds.Add(row.Id);

            this.Helper.WriteConfig(this.Config);
            Game1.playSound("drumkit6");
        }

        this.IsDragging = false;
        this.IsDraggingScrollBar = false;
        this.HasMovedEnough = false;
        this.PotentialClickedIndex = -1;
    }

    public override void draw(SpriteBatch b)
    {
        // 1. Nền mờ phía sau
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.55f);

        // 2. Khung hộp thoại Stardew Valley
        Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

        // 3. Tiêu đề màu trắng nổi bật
        string title = "Radar | Thu Lượm & Dân Làng";
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        Vector2 titlePos = new Vector2(this.xPositionOnScreen + (this.width - titleSize.X) / 2f, this.yPositionOnScreen + 32);
        // Đổ bóng nhẹ phía sau để chữ trắng đọc siêu nét trên mọi nền hộp thoại
        b.DrawString(Game1.dialogueFont, title, titlePos + new Vector2(2, 2), Color.Black * 0.75f);
        b.DrawString(Game1.dialogueFont, title, titlePos, Color.White);

        // 4. Vẽ 2 Tab chọn (Dân làng / Vật phẩm) nằm gọn gàng 100% trong khung
        // Tab 1: Dân làng
        Color tab1Bg = (this.ActiveTab == 0) ? new Color(255, 235, 190) : new Color(175, 160, 140);
        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(64, 320, 64, 64), this.TabVillagersRect.X, this.TabVillagersRect.Y, this.TabVillagersRect.Width, this.TabVillagersRect.Height, tab1Bg, 2f, false);
        string tab1Text = "👥 DÂN LÀNG";
        Vector2 tab1Size = Game1.smallFont.MeasureString(tab1Text);
        Color tab1TextColor = (this.ActiveTab == 0) ? Game1.textColor : new Color(70, 50, 40);
        b.DrawString(Game1.smallFont, tab1Text, new Vector2(this.TabVillagersRect.X + (this.TabVillagersRect.Width - tab1Size.X) / 2f, this.TabVillagersRect.Y + 11), tab1TextColor);

        // Tab 2: Vật phẩm
        Color tab2Bg = (this.ActiveTab == 1) ? new Color(255, 235, 190) : new Color(175, 160, 140);
        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(64, 320, 64, 64), this.TabItemsRect.X, this.TabItemsRect.Y, this.TabItemsRect.Width, this.TabItemsRect.Height, tab2Bg, 2f, false);
        string tab2Text = "🍄 VẬT PHẨM & KHO BÁU";
        Vector2 tab2Size = Game1.smallFont.MeasureString(tab2Text);
        Color tab2TextColor = (this.ActiveTab == 1) ? Game1.textColor : new Color(70, 50, 40);
        b.DrawString(Game1.smallFont, tab2Text, new Vector2(this.TabItemsRect.X + (this.TabItemsRect.Width - tab2Size.X) / 2f, this.TabItemsRect.Y + 11), tab2TextColor);

        // 5. Parallax Scrolling List với Scissor Clipping
        int listTop = this.yPositionOnScreen + 140;
        int listWidth = this.width - 88;
        Rectangle listRect = new Rectangle(this.xPositionOnScreen + 44, listTop, listWidth, ViewportHeight);

        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(64, 320, 64, 64), listRect.X, listRect.Y, listRect.Width, listRect.Height, Color.Wheat * 0.35f, 2f, false);

        RasterizerState prevRaster = b.GraphicsDevice.RasterizerState;
        Rectangle prevScissor = b.GraphicsDevice.ScissorRectangle;

        b.End();
        RasterizerState rState = new RasterizerState { ScissorTestEnable = true };
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rState);
        b.GraphicsDevice.ScissorRectangle = listRect;

        for (int i = 0; i < this.FilteredRows.Count; i++)
        {
            float rowY = listTop + (i * RowHeight) - this.CurrentScrollY;

            if (rowY + RowHeight < listTop || rowY > listTop + ViewportHeight)
                continue;

            var row = this.FilteredRows[i];
            Rectangle boxRect = new Rectangle(listRect.X + 4, (int)rowY + 2, listRect.Width - 8, RowHeight - 6);

            // Nền thẻ item với màu sắc hài hòa, thanh lịch
            Color bgColor = row.Enabled ? (row.IsNpc ? new Color(220, 240, 255) : new Color(215, 248, 220)) : new Color(242, 225, 225);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), boxRect.X, boxRect.Y, boxRect.Width, boxRect.Height, bgColor, 4f, false);

            // Icon Sprite (Item hoặc Mugshot Chân dung Avatar Dân Làng)
            // Chuẩn Mugshot 16x24 được phóng to căn giữa hoàn chỉnh tuyệt đối
            if (row.Texture != null)
            {
                float iconScale = row.IsNpc ? 2.8f : 2.8f;
                int iconHeight = (int)(row.SourceRect.Height * iconScale);
                int iconOffsetY = (boxRect.Height - iconHeight) / 2;
                b.Draw(row.Texture, new Vector2(boxRect.X + 16, boxRect.Y + iconOffsetY), row.SourceRect, Color.White, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0.9f);
            }

            // Nhãn trạng thái BẬT / TẮT (ở góc phải)
            string statusText = row.Enabled ? "BẬT" : "TẮT";
            Color statusColor = row.Enabled ? new Color(0, 130, 0) : Color.DarkRed;
            Vector2 statusSize = Game1.smallFont.MeasureString(statusText);
            int statusX = boxRect.Right - (int)statusSize.X - 18;
            int statusY = boxRect.Y + (int)((boxRect.Height - statusSize.Y) / 2f);

            b.DrawString(Game1.smallFont, statusText, new Vector2(statusX, statusY), statusColor);

            // Tag Danh mục: font chuẩn 1.0f rõ ràng, màu sắc tương phản cao
            string categoryTag = $"[{row.Category}]";
            Vector2 catSize = Game1.smallFont.MeasureString(categoryTag);
            int catX = statusX - (int)catSize.X - 16;
            int catY = boxRect.Y + (int)((boxRect.Height - catSize.Y) / 2f);

            Color catColor = row.IsNpc ? new Color(20, 60, 160) : new Color(20, 110, 40);
            b.DrawString(Game1.smallFont, categoryTag, new Vector2(catX, catY), catColor);

            // Tên item / NPC: hiển thị màu Game1.textColor (màu nâu sẫm truyền thống của game) rõ ràng trên nền sáng
            string displayName = row.Name;
            int textStartX = boxRect.X + 74;
            int maxNameWidth = catX - textStartX - 8;
            Vector2 nameSize = Game1.smallFont.MeasureString(displayName);
            if (nameSize.X > maxNameWidth && maxNameWidth > 50)
            {
                while (displayName.Length > 3 && (Game1.smallFont.MeasureString(displayName + "...").X) > maxNameWidth)
                {
                    displayName = displayName.Substring(0, displayName.Length - 1);
                }
                displayName += "...";
            }
            int nameY = boxRect.Y + (int)((boxRect.Height - nameSize.Y) / 2f);

            b.DrawString(Game1.smallFont, displayName, new Vector2(textStartX, nameY), Game1.textColor);
        }

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, prevRaster);
        b.GraphicsDevice.ScissorRectangle = prevScissor;

        // Vẽ ScrollBar
        float maxScroll = this.GetMaxScroll();
        if (maxScroll > 0)
        {
            int scrollBarX = listRect.Right - 14;
            int barHeight = ViewportHeight;
            float thumbHeight = Math.Max(52f, (float)ViewportHeight / (this.FilteredRows.Count * RowHeight) * ViewportHeight);
            float thumbY = listTop + (this.CurrentScrollY / maxScroll) * (ViewportHeight - thumbHeight);

            b.Draw(Game1.fadeToBlackRect, new Rectangle(scrollBarX, listTop, 10, barHeight), Color.Black * 0.25f);

            Rectangle thumbRect = new Rectangle(scrollBarX, (int)thumbY, 10, (int)thumbHeight);
            b.Draw(Game1.fadeToBlackRect, thumbRect, Color.Goldenrod * 0.9f);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(thumbRect.X + 2, thumbRect.Y + 2, thumbRect.Width - 4, thumbRect.Height - 4), Color.Gold);
        }

        base.draw(b);
        drawMouse(b);
    }
}
