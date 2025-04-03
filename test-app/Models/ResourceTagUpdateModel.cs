using System.ComponentModel.DataAnnotations;

namespace TestApp.Models;

public class ResourceTagUpdateModel
{
    [Required]
    [Display(Name = "Azure Resource ID")]
    public required string ResourceId { get; set; }

    [Required]
    [Display(Name = "Tags (key=value format, one per line)")]
    public required string TagsInput { get; set; }

    public Dictionary<string, string> ParseTags()
    {
        var result = new Dictionary<string, string>();
        
        if (string.IsNullOrWhiteSpace(TagsInput))
            return result;
            
        var lines = TagsInput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = value;
                }
            }
        }
        
        return result;
    }
}
