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
            if (opt.label == "--- FORAGE RADAR ---")
                return;
        }

        // Tiêu đề mục cài đặt
        optionsPage.options.Add(new OptionsElement("--- FORAGE RADAR ---"));

        // Checkbox Bật / Tắt Radar
        var chkEnabled = new ModOptionsCheckbox("Bật Radar tìm kiếm", this.Config.Enabled, val =>
        {
            this.Config.Enabled = val;
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(chkEnabled);

        // Checkbox Bật / Tắt Vòng tròn radar quanh người chơi
        var chkCircle = new ModOptionsCheckbox("Hiện vòng tròn Radar", this.Config.ShowCircle, val =>
        {
            this.Config.ShowCircle = val;
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(chkCircle);

        // Checkbox Bật / Tắt hiển thị khoảng cách
        var chkDist = new ModOptionsCheckbox("Hiện khoảng cách (mét)", this.Config.ShowDistance, val =>
        {
            this.Config.ShowDistance = val;
            this.Helper.WriteConfig(this.Config);
        });
        optionsPage.options.Add(chkDist);

        // Nút mở menu quản lý từng item hái lượm cần quét
        var btnItems = new OptionsButton("Quản lý danh sách Item cần quét...", () =>
        {
            Game1.activeClickableMenu = new RadarMenu(this.Config, this.Helper);
        });
        optionsPage.options.Add(btnItems);
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
