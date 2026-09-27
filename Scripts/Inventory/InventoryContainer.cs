using Godot;
using System.Collections.Generic;

public class InventoryContainer
{
    public InventoryContainerData Data;
    public List<InventoryZone> Zones = new();

    public InventoryContainer(InventoryContainerData data)
    {
        Data = data;
        foreach (var z in data.Zones)
            Zones.Add(new InventoryZone(z));
    }

    // Универсальный поиск места под предмет
    public bool TryAddItem(ItemInstance item, bool allowRotate = true)
    {
        foreach (var zone in Zones)
        {
            if (!string.IsNullOrEmpty(zone.Data.AllowedTag) &&
                zone.Data.AllowedTag != item.Data.Tag)
                continue;

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