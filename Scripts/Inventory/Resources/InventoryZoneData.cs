using Godot;

[GlobalClass]
public partial class InventoryZoneData : Resource
{
    [Export] public string ZoneName = "Main";		   // Название
    [Export] public Vector2I Offset = Vector2I.Zero;   // Смещение внутри контейнера (в клетках)
    [Export] public Vector2I Size = new(4, 4);         // Размер зоны (в клетках)
    [Export] public string AllowedTag = "";            // Разрешённые типы предметов ("" = любые предметы)
}