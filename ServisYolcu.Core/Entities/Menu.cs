namespace ServisYolcu.Core.Entities;

public class Menu
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Path { get; set; } = string.Empty;
    public int Order { get; set; }
    public int? ParentId { get; set; }
    public Menu? Parent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Menu> Children { get; set; } = new List<Menu>();
    public ICollection<MenuRole> MenuRoles { get; set; } = new List<MenuRole>();
}
