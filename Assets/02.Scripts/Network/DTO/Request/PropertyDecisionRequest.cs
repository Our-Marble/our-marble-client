using System;

[Serializable]
public class PropertyDecisionRequest
{
    public RequestType type;
    public int propertyId;
    public bool isAccept;
}
