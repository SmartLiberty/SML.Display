namespace SML.Display.Core.Data.Settings;

using System.ComponentModel.DataAnnotations;

public class InstallSettings
{
    [Required]
    public string Data { get; set; } = "";
}
