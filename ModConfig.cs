using System.Collections.Generic;

namespace ForageRadar;

public class ModConfig
{
    public bool Enabled { get; set; } = true;
    public bool ShowCircle { get; set; } = true;
    public float ScanRadiusTiles { get; set; } = 60f;
    public float RadarCircleRadius { get; set; } = 220f;
    public bool ShowDistance { get; set; } = true;
    public float DistanceFontScale { get; set; } = 1.5f;
    public float IconScale { get; set; } = 2.6f;
    public bool IncludeArtifactSpots { get; set; } = true;
    public bool TrackVillagers { get; set; } = true; // Quét tìm dân làng / NPC

    // Danh sách các ID bị tắt (item hoặc tên NPC)
    public HashSet<string> DisabledItemIds { get; set; } = new();
}
