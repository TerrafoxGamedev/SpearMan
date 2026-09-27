using Godot;
using System.Collections.Generic;

public class InventoryZone
{
    public InventoryZoneData Data;             // Ссылка на шаблон зоны
    public ItemInstance[,] Grid;               // Размер каждого предмета зоны
    public List<ItemInstance> Items = new();   // Список всех предметов зоны

    // Конструктор: cоздаём пустую сетку нужного размера
    public InventoryZone(InventoryZoneData data)
    {
        Data = data;
        Grid = new ItemInstance[data.Size.X, data.Size.Y];
    }

    // Проверяем возможность положить предмет в желаемое место
    public bool CanPlace(ItemData item, Vector2I pos, bool rotated)
    {
        // Сопоставление размера предмета и границ зоны. Если вылезает за зону — отказ
        var size = rotated ? new Vector2I(item.Size.Y, item.Size.X) : item.Size;
        if (pos.X < 0 || pos.Y < 0) return false;
        if (pos.X + size.X > Data.Size.X) return false;
        if (pos.Y + size.Y > Data.Size.Y) return false;

        // Обходим все клетки, которые займёт предмет. Если хоть одна занята — отказ
        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
                if (Grid[pos.X + x, pos.Y + y] != null)
                    return false;

        return true;
    }

    // Помщаем предмет
    public void Place(ItemInstance item, Vector2I pos, bool rotated)
    {
        // Обновляем у предмета: позицию, поворот, зону. И добавляем его в список предметов зоны
        var size = rotated ? new Vector2I(item.Data.Size.Y, item.Data.Size.X) : item.Data.Size;
        item.Position = pos;
        item.Rotated = rotated;
        item.OwnerZone = this;
        Items.Add(item);

        // Помечаем все клетки прямоугольника как пренадлежные конкретному предмету
        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
                Grid[pos.X + x, pos.Y + y] = item;
    }

    // Убираем предмет
    public void Remove(ItemInstance item)
    {
        // Используем эффективный (с учётом поворота) размер предмета
        var size = item.EffectiveSize;

        // Чистим клетки
        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
                Grid[item.Position.X + x, item.Position.Y + y] = null;

        // Убираем из списка предметов и «отвязываем» от зоны
        Items.Remove(item);
        item.OwnerZone = null;
    }
}