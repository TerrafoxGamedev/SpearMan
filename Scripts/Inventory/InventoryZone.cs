using Godot;
using System.Collections.Generic;

public class InventoryZone
{
    public InventoryZoneData Data;
    public ItemInstance[,] Grid;
    public List<ItemInstance> Items = new();

    public InventoryZone(InventoryZoneData data)
    {
        Data = data;
        Grid = new ItemInstance[data.Size.X, data.Size.Y];
    }

    public bool CanPlace(ItemData item, Vector2I pos, bool rotated)
    {
        var size = rotated ? new Vector2I(item.Size.Y, item.Size.X) : item.Size;
        if (pos.X < 0 || pos.Y < 0) return false;
        if (pos.X + size.X > Data.Size.X) return false;
        if (pos.Y + size.Y > Data.Size.Y) return false;

        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
                if (Grid[pos.X + x, pos.Y + y] != null)
                    return false;

        return true;
    }

    public void Place(ItemInstance item, Vector2I pos, bool rotated)
    {
        var size = rotated ? new Vector2I(item.Data.Size.Y, item.Data.Size.X) : item.Data.Size;
        item.Position = pos;
        item.Rotated = rotated;
        item.OwnerZone = this;
        Items.Add(item);

        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
                Grid[pos.X + x, pos.Y + y] = item;
    }

    public void Remove(ItemInstance item)
    {
        var size = item.EffectiveSize;
        for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
                Grid[item.Position.X + x, item.Position.Y + y] = null;

        Items.Remove(item);
        item.OwnerZone = null;
    }
}