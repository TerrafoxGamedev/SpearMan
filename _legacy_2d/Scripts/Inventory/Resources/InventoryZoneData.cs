using Godot;

[GlobalClass]
public partial class InventoryZoneData : Resource
{
    [Export] public string ZoneName = "Main";		   // Название
    [Export] public Vector2I Offset = Vector2I.Zero;   // Смещение внутри контейнера (в клетках)
    [Export] public Vector2I Size = new(4, 4);         // Размер зоны (в клетках)
    [Export] public Godot.Collections.Array<string> AllowedTags = new();   // Разрешённые типы предметов ("" = любые предметы)

    // Проверяем, принимает ли зона предмет с такими тегами
    public bool Accepts(ItemData item)
    {
        if (AllowedTags == null || AllowedTags.Count == 0)
            return true;    // У зоны нет тегов — подходит любой предмет

        if (item.Tags == null || item.Tags.Count == 0)
            return false;   // У предмета нет тегов — не подходит под ограниченную зону

        foreach (var t in item.Tags)
            if (AllowedTags.Contains(t))
                return true;

        return false;
    }
}