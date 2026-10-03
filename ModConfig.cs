using System.Collections.Generic;

namespace ForageRadar;

public class ModConfig
{
    public bool Enabled { get; set; } = true;
    public bool ShowCircle { get; set; } = true;
    public float ScanRadiusTiles { get; set; } = 40f;
    public float RadarCircleRadius { get; set; } = 120f;
    public bool ShowDistance { get; set; } = true;
    public float DistanceFontScale { get; set; } = 0.85f;
    public float IconScale { get; set; } = 2.0f;
    public bool IncludeArtifactSpots { get; set; } = true;
    public bool TrackVillagers { get; set; } = true; // Quét tìm dân làng / NPC

    // Danh sách các ID bị tắt (item hoặc tên NPC)
    public HashSet<string> DisabledItemIds { get; set; } = new();
}
