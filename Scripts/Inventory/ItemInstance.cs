using Godot;

public class ItemInstance
{
    public ItemData Data;
    public int Count = 1;
    public bool Rotated = false;
    public Vector2I Position;      // Позиция внутри зоны (в клетках)
    public InventoryZone OwnerZone;

    public ItemInstance(ItemData data) { Data = data; }

    // Эффективный размер с учётом поворота
    public Vector2I EffectiveSize =>
        Rotated ? new Vector2I(Data.Size.Y, Data.Size.X) : Data.Size;
}