namespace ServisYolcu.Core.DTOs.Menu;

public class MenuDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Path { get; set; } = string.Empty;
    public int Order { get; set; }
    public int? ParentId { get; set; }
    public List<MenuDto> Children { get; set; } = new();
}
