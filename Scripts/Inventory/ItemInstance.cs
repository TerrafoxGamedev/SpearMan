using Godot;

public class ItemInstance
{
    public ItemData Data;           // Ссылка на шаблон предмета
    public int Count = 1;           // Текущее количество предметов в стаке
    public bool Rotated = false;    // Повёрнут ли предмет
    public Vector2I Position;       // Позиция левого-верхнего угла предмета внутри своей зоны (в клетках)
    public InventoryZone OwnerZone; // Обратная ссылка на зону в которой сейчас лежит предмет

    public ItemInstance(ItemData data) { Data = data; }   // Конструктор: создаём предмет

    // Эффективный размер с учётом поворота
    public Vector2I EffectiveSize =>
        Rotated ? new Vector2I(Data.Size.Y, Data.Size.X) : Data.Size;
}