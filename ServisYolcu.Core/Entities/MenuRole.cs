using ServisYolcu.Core.Enums;

namespace ServisYolcu.Core.Entities;

public class MenuRole
{
    public int Id { get; set; }
    public int MenuId { get; set; }
    public Menu Menu { get; set; } = null!;
    public UserRole Role { get; set; }
}
