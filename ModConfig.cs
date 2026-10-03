namespace ForageRadar;

public class ModConfig
{
    public bool Enabled { get; set; } = true;
    public float ScanRadiusTiles { get; set; } = 40f;
    public float RadarCircleRadius { get; set; } = 130f;
    public bool ShowDistance { get; set; } = true;
    public float IconScale { get; set; } = 2.0f;
    public bool IncludeArtifactSpots { get; set; } = true;
}
