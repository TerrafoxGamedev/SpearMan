using Godot;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string Id = "item_id";			// ID (Не обязательно цифры)
    [Export] public string DisplayName = "Item";	// Название
    [Export] public Texture2D Icon;					// Икона
    [Export] public Vector2I Size = new(1, 1);   	// Сколько клеток занимает
    [Export] public Godot.Collections.Array<string> Tags = new();  	// ["Тэги"]
    [Export] public int MaxStack = 1;				// Размер стака
    [Export] public bool Rotatable = true;			// Возможность поворота
}