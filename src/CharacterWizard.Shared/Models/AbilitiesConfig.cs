namespace CharacterWizard.Shared.Models;

public class PointBuyCost
{
    public int Score { get; set; }
    public int Cost { get; set; }
}

public class PointBuyConfig
{
    public int Budget { get; set; }
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
    public List<PointBuyCost> Costs { get; set; } = [];
}

public class AbilitiesConfig
{
    public string SchemaVersion { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<int> StandardArray { get; set; } = [];
    public PointBuyConfig PointBuy { get; set; } = new();
}
