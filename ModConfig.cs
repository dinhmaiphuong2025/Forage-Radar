using System.Collections.Generic;

namespace ForageRadar;

public class ModConfig
{
    public bool Enabled { get; set; } = true;
    public float ScanRadiusTiles { get; set; } = 40f;
    public float RadarCircleRadius { get; set; } = 120f;
    public bool ShowDistance { get; set; } = true;
    public float IconScale { get; set; } = 2.0f;
    public bool IncludeArtifactSpots { get; set; } = true;

    // Danh sách các QualifiedItemId bị tắt (không muốn radar tìm)
    public HashSet<string> DisabledItemIds { get; set; } = new();

    // Vị trí nút bật menu radar trên màn hình
    public int ButtonPositionX { get; set; } = 20;
    public int ButtonPositionY { get; set; } = 180;
}
