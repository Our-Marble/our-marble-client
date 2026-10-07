using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum BroadcastType
{
    [EnumMember(Value = "AWAITING_ROLL")]
    DICE_ROLLED,
    
    [EnumMember(Value = "AWAITING_DESTINATION")]
    DESTINATION_CHOSEN,
    
    [EnumMember(Value = "AWAITING_PURCHASE")]
    PROPERTY_PURCHASED,
    
    [EnumMember(Value = "AWAITING_BUILD")]
    BUILT,
    
    [EnumMember(Value = "AWAITING_ACQUIRE")]
    PROPERTY_ACQUIRED,
    
    [EnumMember(Value = "AWAITING_DRAW")]
    CARD_DRAWN,
    
    [EnumMember(Value = "AWAITING_SELL")]
    PROPERTIES_SOLD
}