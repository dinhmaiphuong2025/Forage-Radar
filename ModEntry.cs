using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace ForageRadar;

public class ModEntry : Mod
{
    private ModConfig Config = null!;
    private RadarRenderer Renderer = null!;
    private List<TrackedItem> TrackedItems = new();
    private int TickCounter = 0;

    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>();
        this.Renderer = new RadarRenderer(this.Config);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;

        this.Monitor.Log("Forage Radar đã khởi động sẵn sàng.", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!this.Config.Enabled || !Context.IsWorldReady || Game1.currentLocation == null)
        {
            this.TrackedItems.Clear();
            return;
        }

        // Quét lại vị trí mỗi 15 ticks (~0.25s) để tiết kiệm CPU mà vẫn mượt mà theo thời gian thực
        if (++this.TickCounter >= 15)
        {
            this.TickCounter = 0;
            this.TrackedItems = ForageScanner.Scan(Game1.currentLocation, Game1.player, this.Config);
        }
    }

    // Vẽ radar trong không gian World: ghim chuẩn xác tuyệt đối quanh người chơi dù camera đứng yên ở cổng hay góc map
    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!this.Config.Enabled || !Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu != null)
            return;

        this.Renderer.DrawWorld(e.SpriteBatch, this.TrackedItems, Game1.player);
    }

    // Tích hợp hoàn toàn vào Cài đặt (Options) lớn của game Stardew Valley
    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is GameMenu gameMenu)
        {
            foreach (var page in gameMenu.pages)
            {
                if (page is OptionsPage optionsPage)
                {
                    this.InjectRadarOptions(optionsPage);
                }
            }
        }
    }

    private void InjectRadarOptions(OptionsPage optionsPage)
    {
        foreach (var opt in optionsPage.options)
        {
            if (opt.label == "--- FORAGE & NPC RADAR ---")
                return;
        }

        // Tiêu đề mục cài đặt
        optionsPage.options.Add(new OptionsElement("--- FORAGE & NPC RADAR ---"));

        // 1. Checkbox Bật / Tắt Radar
        var chkEnabled = new ModOptionsCheckbox("Bật Radar tìm kiếm", this.Config.Enabled, val =>
        {
            this.Config.Enabled = val;
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(chkEnabled);

        // 2. Checkbox Bật / Tắt Vòng tròn radar quanh người chơi
        var chkCircle = new ModOptionsCheckbox("Hiện vòng tròn Radar", this.Config.ShowCircle, val =>
        {
            this.Config.ShowCircle = val;
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(chkCircle);

        // 3. Tùy chọn Bán kính vòng tròn hiển thị trên màn hình (Radar Circle Size)
        List<string> circleOptions = new() { "Nhỏ (90px)", "Vừa (120px)", "Lớn (150px)", "Rất lớn (180px)" };
        int initialCircleIdx = this.Config.RadarCircleRadius switch
        {
            <= 90f => 0,
            <= 120f => 1,
            <= 150f => 2,
            _ => 3
        };
        var pmCircle = new ModOptionsPlusMinus("Bán kính vòng tròn:", initialCircleIdx, circleOptions, idx =>
        {
            this.Config.RadarCircleRadius = idx switch
            {
                0 => 90f,
                1 => 120f,
                2 => 150f,
                _ => 180f
            };
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(pmCircle);

        // 4. Tùy chọn Bán kính quét trên bản đồ (Scan Range Tiles)
        List<string> scanOptions = new() { "Gần (20 ô)", "Vừa (40 ô)", "Rộng (60 ô)", "Toàn map (100 ô)" };
        int initialScanIdx = this.Config.ScanRadiusTiles switch
        {
            <= 20f => 0,
            <= 40f => 1,
            <= 60f => 2,
            _ => 3
        };
        var pmScan = new ModOptionsPlusMinus("Bán kính quét map:", initialScanIdx, scanOptions, idx =>
        {
            this.Config.ScanRadiusTiles = idx switch
            {
                0 => 20f,
                1 => 40f,
                2 => 60f,
                _ => 100f
            };
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(pmScan);

        // 5. Checkbox Bật / Tắt Dò tìm Dân Làng
        var chkVillagers = new ModOptionsCheckbox("Dò tìm Dân Làng (NPC)", this.Config.TrackVillagers, val =>
        {
            this.Config.TrackVillagers = val;
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(chkVillagers);

        // 6. Checkbox Bật / Tắt hiển thị khoảng cách
        var chkDist = new ModOptionsCheckbox("Hiện khoảng cách (mét)", this.Config.ShowDistance, val =>
        {
            this.Config.ShowDistance = val;
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(chkDist);

        // 7. Nút mở menu quản lý từng item hái lượm & dân làng cần quét
        var btnItems = new OptionsButton("Quản lý danh sách Item & Dân Làng...", () =>
        {
            Game1.activeClickableMenu = new RadarMenu(this.Config, this.Helper);
        });
        optionsPage.options.Add(btnItems);
    }
}

// Lớp hỗ trợ nút tăng giảm PlusMinus mượt mà trong OptionsPage
public class ModOptionsPlusMinus : OptionsElement
{
    private readonly List<string> Options;
    private readonly Action<int> OnChanged;
    public int selected;
    private Rectangle minusButton;
    private Rectangle plusButton;

    public ModOptionsPlusMinus(string label, int initialIndex, List<string> options, Action<int> onChanged)
        : base(label, -1, -1, 32, 32, -1)
    {
        this.Options = options;
        this.selected = Math.Clamp(initialIndex, 0, options.Count - 1);
        this.OnChanged = onChanged;
    }

    public override void receiveLeftClick(int x, int y)
    {
        if (this.minusButton.Contains(x, y) && this.selected > 0)
        {
            this.selected--;
            Game1.playSound("drumkit6");
            this.OnChanged(this.selected);
        }
        else if (this.plusButton.Contains(x, y) && this.selected < this.Options.Count - 1)
        {
            this.selected++;
            Game1.playSound("drumkit6");
            this.OnChanged(this.selected);
        }
    }

    public override void draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        int labelWidth = (int)Game1.dialogueFont.MeasureString(this.label).X;
        int startX = slotX + this.bounds.X + labelWidth + 24;
        int itemY = slotY + this.bounds.Y;

        this.minusButton = new Rectangle(startX, itemY, 28, 32);
        b.Draw(Game1.mouseCursors, new Vector2(this.minusButton.X, this.minusButton.Y), new Rectangle(177, 345, 7, 8), this.selected > 0 ? Color.White : Color.Gray * 0.5f, 0f, Vector2.Zero, 4f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0.4f);

        string currentText = this.Options[this.selected];
        Vector2 textSize = Game1.dialogueFont.MeasureString(currentText);
        b.DrawString(Game1.dialogueFont, currentText, new Vector2(startX + 36, itemY - 4), Game1.textColor);

        this.plusButton = new Rectangle(startX + 44 + (int)textSize.X, itemY, 28, 32);
        b.Draw(Game1.mouseCursors, new Vector2(this.plusButton.X, this.plusButton.Y), new Rectangle(184, 345, 7, 8), this.selected < this.Options.Count - 1 ? Color.White : Color.Gray * 0.5f, 0f, Vector2.Zero, 4f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0.4f);

        base.draw(b, slotX, slotY, context);
    }
}

// Lớp hỗ trợ checkbox tuỳ chỉnh phản hồi tương tác mượt mà trong OptionsPage
public class ModOptionsCheckbox : OptionsElement
{
    public bool isChecked;
    private readonly Action<bool> OnToggled;

    public ModOptionsCheckbox(string label, bool initialValue, Action<bool> onToggled)
        : base(label, -1, -1, 36, 36, -1)
    {
        this.isChecked = initialValue;
        this.OnToggled = onToggled;
    }

    public override void receiveLeftClick(int x, int y)
    {
        Game1.playSound("drumkit6");
        base.receiveLeftClick(x, y);
        this.isChecked = !this.isChecked;
        this.OnToggled(this.isChecked);
    }

    public override void draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        b.Draw(
            Game1.mouseCursors,
            new Vector2(slotX + this.bounds.X, slotY + this.bounds.Y),
            new Rectangle(this.isChecked ? 236 : 227, 425, 9, 9),
            Color.White,
            0f,
            Vector2.Zero,
            4f,
            Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
            0.4f
        );

        base.draw(b, slotX, slotY, context);
    }
}
