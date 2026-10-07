using System;
using System.Collections.Generic;

[Serializable]
public class SellPropertiesRequest
{
    public RequestType type;
    public List<int> propertyIds;
}
