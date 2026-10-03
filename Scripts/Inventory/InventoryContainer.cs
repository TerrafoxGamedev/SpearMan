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
        // 1) Попытка застакать предметы
        if (TryStackExisting(item))
            return true;
        
        // 2) Идём по зонам в порядке добавления если не застакали
        foreach (var zone in Zones)
        {
            // Фильтр по тегу
            if (!zone.Data.Accepts(item.Data))
                continue;

            // Пробуем каждую клетку как левый-верхний угол 
            // Проход 1: БЕЗ поворота — ищем везде
            if (TryPlaceInZone(zone, item, rotated: false))
                return true;

            // Проход 2: С поворотом — только если разрешено
            if (allowRotate && item.Data.Rotatable &&
                TryPlaceInZone(zone, item, rotated: true))
                return true;
            }
        return false;
    }

    // Ищет неполный стак предмета с тем же Id. Возвращает true, если удалось полностью объединить стаки.
    private bool TryStackExisting(ItemInstance item)
    {
        if (item.Data.MaxStack <= 1) return false;   // не стакается — искать нечего

        foreach (var zone in Zones)
        {
            if (!zone.Data.Accepts(item.Data)) continue;

            foreach (var existing in zone.Items)
            {
                if (existing.Data.Id != item.Data.Id) continue;
                if (existing.Count >= existing.Data.MaxStack) continue;

                int free = existing.Data.MaxStack - existing.Count;
                int move = Mathf.Min(free, item.Count);

                existing.Count += move;
                item.Count -= move;

                if (item.Count <= 0)
                    return true;   // весь предмет «утонул» в стаке
            }
        }
        return item.Count == 0;   // Если не влез – распределили по нескольким стакам
    }

    private bool TryPlaceInZone(InventoryZone zone, ItemInstance item, bool rotated)
    {
        for (int x = 0; x < zone.Data.Size.X; x++)
            for (int y = 0; y < zone.Data.Size.Y; y++)
            {
                var p = new Vector2I(x, y);
                if (zone.CanPlace(item.Data, p, rotated))
                {
                    zone.Place(item, p, rotated);
                    return true;
                }
            }
        return false;
    }
}