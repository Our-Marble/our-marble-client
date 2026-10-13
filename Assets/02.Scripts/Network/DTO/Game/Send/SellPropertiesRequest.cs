using System;
using System.Collections.Generic;

[Serializable]
public class SellPropertiesRequest
{
    public string type;
    public List<int> propertyIds;
}
