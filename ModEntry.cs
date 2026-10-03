using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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
        helper.Events.Display.RenderedHud += this.OnRenderedHud;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;

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

    // Vẽ nút UI trên màn hình HUD
    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!this.Config.Enabled || !Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu != null)
            return;

        this.Renderer.DrawHud(e.SpriteBatch);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
            return;

        // Bắt mọi nút chạm cảm ứng / click chuột
        if (e.Button.IsUseToolButton() || e.Button == SButton.MouseLeft)
        {
            // Kiểm tra cả tọa độ ScreenPixels từ SMAPI Cursor và Game1 Mouse
            ICursorPosition cursor = this.Helper.Input.GetCursorPosition();
            Point cursorPt = new Point((int)cursor.ScreenPixels.X, (int)cursor.ScreenPixels.Y);
            Point mousePt = Game1.getMousePosition();

            if (this.Renderer.ButtonBounds.Contains(cursorPt) || this.Renderer.ButtonBounds.Contains(mousePt))
            {
                this.Helper.Input.Suppress(e.Button);
                Game1.playSound("bigSelect");
                Game1.activeClickableMenu = new RadarMenu(this.Config, this.Helper);
            }
        }
    }
}
