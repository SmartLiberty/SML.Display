namespace SML.Display.Core.Data.Settings;

using System.ComponentModel.DataAnnotations;

public class SiteSettings
{
    [Required]
    [StringLength(5, MinimumLength = 2)]
    public string Language { get; set; } = "";
}
