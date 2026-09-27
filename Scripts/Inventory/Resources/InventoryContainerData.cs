using Godot;
using Godot.Collections;

[GlobalClass]
public partial class InventoryContainerData : Resource
{
    [Export] public string DisplayName = "Backpack";          // Название контейнера
    [Export] public Texture2D Icon;                           // Иконка
    [Export] public Array<InventoryZoneData> Zones = new();   // Массив зон
}