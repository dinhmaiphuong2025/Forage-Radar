using System.Collections.Generic;

namespace ForageRadar;

public class ModConfig
{
    public bool Enabled { get; set; } = true;
    public bool ShowCircle { get; set; } = true; // Tùy chọn Bật/Tắt vẽ vòng tròn radar
    public float ScanRadiusTiles { get; set; } = 40f;
    public float RadarCircleRadius { get; set; } = 120f;
    public bool ShowDistance { get; set; } = true;
    public float DistanceFontScale { get; set; } = 0.85f; // Tùy chọn kích thước font chữ khoảng cách (rõ ràng, dễ đọc)
    public float IconScale { get; set; } = 2.0f;
    public bool IncludeArtifactSpots { get; set; } = true;

    // Danh sách các QualifiedItemId bị tắt (không muốn radar tìm)
    public HashSet<string> DisabledItemIds { get; set; } = new();
}
