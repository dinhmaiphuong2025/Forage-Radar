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

        if (++this.TickCounter >= 15)
        {
            this.TickCounter = 0;
            this.TrackedItems = ForageScanner.Scan(Game1.currentLocation, Game1.player, this.Config);
        }
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!this.Config.Enabled || !Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu != null)
            return;

        this.Renderer.DrawWorld(e.SpriteBatch, this.TrackedItems, Game1.player);
    }

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
        List<string> circleOptions = new() { "Vừa (160px)", "Lớn (220px)", "Rất lớn (280px)", "Cực đại (350px)" };
        int initialCircleIdx = this.Config.RadarCircleRadius switch
        {
            <= 160f => 0,
            <= 220f => 1,
            <= 280f => 2,
            _ => 3
        };
        var pmCircle = new ModOptionsPlusMinus("Bán kính vòng tròn", initialCircleIdx, circleOptions, idx =>
        {
            this.Config.RadarCircleRadius = idx switch
            {
                0 => 160f,
                1 => 220f,
                2 => 280f,
                _ => 350f
            };
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(pmCircle);

        // 4. Tùy chọn Bán kính quét trên bản đồ
        List<string> scanOptions = new() { "Gần (20 ô)", "Vừa (40 ô)", "Rộng (60 ô)", "Toàn map (100 ô)" };
        int initialScanIdx = this.Config.ScanRadiusTiles switch
        {
            <= 20f => 0,
            <= 40f => 1,
            <= 60f => 2,
            _ => 3
        };
        var pmScan = new ModOptionsPlusMinus("Bán kính quét map", initialScanIdx, scanOptions, idx =>
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

        // 6. Tùy chọn Kích thước Icon & Avatar (Icon Size) - Mặc định 2.6x
        List<string> iconOptions = new() { "Vừa (2.0x)", "Lớn (2.6x)", "Rất lớn (3.2x)", "Cực đại (4.0x)" };
        int initialIconIdx = this.Config.IconScale switch
        {
            <= 2.0f => 0,
            <= 2.6f => 1,
            <= 3.2f => 2,
            _ => 3
        };
        var pmIcon = new ModOptionsPlusMinus("Cỡ Icon & Avatar", initialIconIdx, iconOptions, idx =>
        {
            this.Config.IconScale = idx switch
            {
                0 => 2.0f,
                1 => 2.6f,
                2 => 3.2f,
                _ => 4.0f
            };
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(pmIcon);

        // 7. Checkbox Bật / Tắt hiển thị khoảng cách
        var chkDist = new ModOptionsCheckbox("Hiện khoảng cách (mét)", this.Config.ShowDistance, val =>
        {
            this.Config.ShowDistance = val;
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(chkDist);

        // 8. Tùy chọn Cỡ chữ khoảng cách (Distance Text Size) - Mặc định 1.5x, hỗ trợ mốc to hơn 2.0x & 2.5x
        List<string> fontOptions = new() { "Vừa (1.2x)", "Lớn (1.5x)", "Rất lớn (2.0x)", "Cực đại (2.5x)" };
        int initialFontIdx = this.Config.DistanceFontScale switch
        {
            <= 1.2f => 0,
            <= 1.5f => 1,
            <= 2.0f => 2,
            _ => 3
        };
        var pmFont = new ModOptionsPlusMinus("Cỡ chữ khoảng cách", initialFontIdx, fontOptions, idx =>
        {
            this.Config.DistanceFontScale = idx switch
            {
                0 => 1.2f,
                1 => 1.5f,
                2 => 2.0f,
                _ => 2.5f
            };
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(pmFont);

        // 9. Nút mở menu quản lý danh sách item & dân làng
        var btnItems = new OptionsButton("Quản lý danh sách Item & Dân Làng...", () =>
        {
            Game1.activeClickableMenu = new RadarMenu(this.Config, this.Helper);
        });
        optionsPage.options.Add(btnItems);
    }
}

// Lớp hỗ trợ nút tăng giảm PlusMinus chuẩn theo OptionsPlusMinus gốc của Stardew Valley
public class ModOptionsPlusMinus : OptionsElement
{
    private readonly List<string> Options;
    private readonly Action<int> OnChanged;
    public int selected;
    private Rectangle minusButton;
    private Rectangle plusButton;

    public ModOptionsPlusMinus(string label, int initialIndex, List<string> options, Action<int> onChanged)
        : base(label, -1, -1, 28, 28, -1)
    {
        this.Options = options;
        this.selected = Math.Clamp(initialIndex, 0, options.Count - 1);
        this.OnChanged = onChanged;

        // Tính max width của text options để tạo bounds bao trọn cả nút minus, text và nút plus
        int maxTextWidth = 0;
        foreach (var opt in options)
        {
            int w = (int)Game1.dialogueFont.MeasureString(opt).X;
            if (w > maxTextWidth) maxTextWidth = w;
        }

        // bounds chứa toàn bộ cụm điều khiển: minus (28px) + space (16px) + text (maxTextWidth) + space (16px) + plus (28px)
        int totalControlWidth = 28 + 16 + maxTextWidth + 16 + 28;
        this.bounds = new Rectangle(32, 16, totalControlWidth, 32);

        this.minusButton = new Rectangle(32, 16, 28, 32);
        this.plusButton = new Rectangle(this.bounds.Right - 28, 16, 28, 32);
    }

    public override void receiveLeftClick(int x, int y)
    {
        // Hitbox mở rộng cho cảm ứng
        Rectangle minusHit = new Rectangle(this.minusButton.X - 16, this.minusButton.Y - 16, this.minusButton.Width + 32, this.minusButton.Height + 32);
        Rectangle plusHit = new Rectangle(this.plusButton.X - 16, this.plusButton.Y - 16, this.plusButton.Width + 32, this.plusButton.Height + 32);

        if (minusHit.Contains(x, y) && this.selected > 0)
        {
            this.selected--;
            Game1.playSound("drumkit6");
            this.OnChanged(this.selected);
        }
        else if (plusHit.Contains(x, y) && this.selected < this.Options.Count - 1)
        {
            this.selected++;
            Game1.playSound("drumkit6");
            this.OnChanged(this.selected);
        }
    }

    public override void draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        int itemY = slotY + this.minusButton.Y;

        // Vẽ nút Minus
        b.Draw(
            Game1.mouseCursors,
            new Vector2(slotX + this.minusButton.X, itemY),
            new Rectangle(177, 345, 7, 8),
            this.selected > 0 ? Color.White : (Color.Gray * 0.5f),
            0f,
            Vector2.Zero,
            4f,
            Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
            0.4f
        );

        // Vẽ text giá trị tùy chọn (căn giữa giữa 2 nút minus và plus)
        string currentText = this.Options[this.selected];
        int textWidth = (int)Game1.dialogueFont.MeasureString(currentText).X;
        int availableSpace = this.plusButton.X - this.minusButton.Right;
        int textX = slotX + this.minusButton.Right + (availableSpace - textWidth) / 2;

        b.DrawString(
            Game1.dialogueFont,
            currentText,
            new Vector2(textX, itemY - 6),
            Game1.textColor
        );

        // Vẽ nút Plus
        b.Draw(
            Game1.mouseCursors,
            new Vector2(slotX + this.plusButton.X, itemY),
            new Rectangle(184, 345, 7, 8),
            this.selected < this.Options.Count - 1 ? Color.White : (Color.Gray * 0.5f),
            0f,
            Vector2.Zero,
            4f,
            Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
            0.4f
        );

        // Vẽ nhãn setting bên phải nút Plus (dialogueFont chuẩn gốc game 100%)
        Color textColor = this.greyedOut ? (Game1.textColor * 0.33f) : Game1.textColor;
        b.DrawString(
            Game1.dialogueFont,
            this.label,
            new Vector2(slotX + this.bounds.Right + 20, itemY - 6),
            textColor
        );
    }
}

// Lớp hỗ trợ checkbox chuẩn theo OptionsCheckbox gốc của Stardew Valley
public class ModOptionsCheckbox : OptionsElement
{
    public bool isChecked;
    private readonly Action<bool> OnToggled;

    public ModOptionsCheckbox(string label, bool initialValue, Action<bool> onToggled)
        : base(label, 32, 16, 36, 36, -1)
    {
        this.isChecked = initialValue;
        this.OnToggled = onToggled;
    }

    public override void receiveLeftClick(int x, int y)
    {
        Game1.playSound("drumkit6");
        this.isChecked = !this.isChecked;
        this.OnToggled(this.isChecked);
    }

    public override void draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        // 1. Vẽ ô tích vuông 36x36px
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

        // 2. Vẽ nhãn bằng dialogueFont chuẩn gốc game 100%
        Color textColor = this.greyedOut ? (Game1.textColor * 0.33f) : Game1.textColor;
        b.DrawString(
            Game1.dialogueFont,
            this.label,
            new Vector2(slotX + this.bounds.X + this.bounds.Width + 16, slotY + this.bounds.Y - 6),
            textColor
        );
    }
}
