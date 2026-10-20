public class PropertyState
{
    public int PropertyId { get; set; }
    public long? OwnerId { get; set; }
    public BuildingLevel BuildingLevel { get; set; }

    public PropertyState(int propertyId)
    {
        PropertyId = propertyId;
        OwnerId = null;
        BuildingLevel = BuildingLevel.Land;
    }
}