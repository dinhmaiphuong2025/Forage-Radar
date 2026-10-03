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

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!this.Config.Enabled || !Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu != null)
            return;

        this.Renderer.Draw(e.SpriteBatch, this.TrackedItems, Game1.player);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
            return;

        // Bắt sự kiện chạm cảm ứng trên màn hình hoặc click chuột
        if (e.Button == SButton.MouseLeft)
        {
            Point mousePos = Game1.getMousePosition();
            if (this.Renderer.ButtonBounds.Contains(mousePos.X, mousePos.Y))
            {
                this.Helper.Input.Suppress(SButton.MouseLeft);
                Game1.playSound("bigSelect");
                Game1.activeClickableMenu = new RadarMenu(this.Config, this.Helper);
            }
        }
    }
}
