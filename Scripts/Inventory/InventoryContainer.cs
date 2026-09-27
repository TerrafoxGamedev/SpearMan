using Godot;
using System.Collections.Generic;

public class InventoryContainer
{
    public InventoryContainerData Data;         // Ссылка на шаблон контейнера
    public List<InventoryZone> Zones = new();   // Список всех зон контейнера

    // Конструктор: cоздаём контейнер с нужными зонами
    public InventoryContainer(InventoryContainerData data)
    {
        Data = data;
        foreach (var z in data.Zones)
            Zones.Add(new InventoryZone(z));
    }

    // Универсальный поиск места под предмет во всём контейнере
    public bool TryAddItem(ItemInstance item, bool allowRotate = true)
    {
        // Идём по зонам в порядке добавления
        foreach (var zone in Zones)
        {
            // Фильтр по тегу
            if (!zone.Data.Accepts(item.Data))
                continue;

            // Пробуем каждую клетку как левый-верхний угол 
            // Сначала без поворота, потом, если не влезло, с поворотом
            for (int x = 0; x < zone.Data.Size.X; x++)
                for (int y = 0; y < zone.Data.Size.Y; y++)
                {
                    var p = new Vector2I(x, y);
                    if (zone.CanPlace(item.Data, p, false))
                    {
                        zone.Place(item, p, false);
                        return true;
                    }
                    if (allowRotate && item.Data.Rotatable &&
                        zone.CanPlace(item.Data, p, true))
                    {
                        zone.Place(item, p, true);
                        return true;
                    }
                }
        }
        return false;
    }
}