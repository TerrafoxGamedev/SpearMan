using Godot;

public enum ItemCollisionShape
{
	Cube,
	Sphere,
	Cylinder,
	OpenCube,
	OpenCylinder
}

[GlobalClass]
public partial class ItemData : Resource
{
	[Export] public string Id = "item_id";
	[Export] public string DisplayName = "Item";
	// Сцена модели, обычно .glb из Models/Items. Пусто — серый куб.
	[Export] public PackedScene Model;
	[Export] public Vector2I Size = new(1, 1);
	[Export] public Godot.Collections.Array<string> Tags = new();
	[Export] public int MaxStack = 1;
	[Export] public bool Rotatable = true;
	// Масса в килограммах. Её задаёт автор предмета.
	[Export] public float Mass = 1f;
	[Export] public ItemCollisionShape CollisionShape = ItemCollisionShape.Cube;
	// Полный размер формы в метрах. Оси правятся отдельно.
	[Export] public Vector3 CollisionSize = new(0.25f, 0.25f, 0.25f);
}
