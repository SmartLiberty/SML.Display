namespace SML.Display.Core.Data.Storable;

using SML.Display.Core.Data.Enums;
using SML.Display.Core.Database;
using System.Collections.Generic;

public class VisitConfig : AuditableEntity
{
    public long Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public bool Archived { get; set; }
    public string? Description { get; set; }
    public List<int> CareUnitIds { get; set; } = [];
    public bool ShowTrombinoscope { get; set; }
    public bool ShowWhoIsWorking { get; set; }
    public bool HideAlarm { get; set; }
    public bool ShowResidentSearch { get; set; }
    public bool AlwaysHideLocationInRealTime { get; set; }
    public Screens DefaultScreen { get; set; }
}
