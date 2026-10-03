using System.Collections.Generic;
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

        this.Monitor.Log("Forage Radar đã khởi động sẵn sàng.", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Có thể tích hợp Generic Mod Config Menu ở đây sau nếu muốn
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
}
