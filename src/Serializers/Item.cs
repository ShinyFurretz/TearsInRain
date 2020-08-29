using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TearsInRain.Entities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace TearsInRain.Serializers {
    public class ItemJsonConverter : JsonConverter<Item> {
        public override void WriteJson(JsonWriter writer, Item value, JsonSerializer serializer) {
            serializer.Serialize(writer, (ItemSerialized)value);
        }

        public override Item ReadJson(JsonReader reader, Type objectType, Item existingValue, 
                                        bool hasExistingValue, JsonSerializer serializer) {
            return serializer.Deserialize<ItemSerialized>(reader);
        }
    }

    /// <summary>
    /// Serialized instance of a <see cref="Item"/>.
    /// </summary>
    [DataContract]
    public class ItemSerialized {
        [DataMember] public string ID; // Item ID for referencing
        [DataMember] public string Name; // Item Name
        [DataMember] public string NamePlural; // Item Name, Plural

        [DataMember] public uint FG; // Foreground
        [DataMember] public uint BG; // Background
        [DataMember] public int Glyph; // Glyph

        [DataMember] public double Weight; // Item Weight in kilograms
        [DataMember] public int Condition; // Item Condition (out of 100)
        [DataMember] public int Slot; // Slot that item can be equipped to. If no applicable slot, this should be -1
        [DataMember] public int Quantity; // Number of items
        [DataMember] public int X;
        [DataMember] public int Y;
        [DataMember] public string Type;

        [DataMember] public Dictionary<string, string> Properties;

        public static implicit operator ItemSerialized(Item item) {
            Color tempFG = item.Animation.CurrentFrame[0].Foreground;
            Color tempBG = item.Animation.CurrentFrame[0].Background;

            var sObj = new ItemSerialized() {
                FG = item.Animation.CurrentFrame[0].Foreground.PackedValue,
                BG = item.Animation.CurrentFrame[0].Background.PackedValue,
                Glyph = item.Animation.CurrentFrame[0].Glyph,
                ID = item._id,
                Name = item.Name,
                NamePlural = item.NamePlural,
                Weight = item.Weight,
                Condition = item.Condition,
                Slot = item.Slot,
                Quantity = item.Quantity,
                Properties = item.Properties,
                X = item.Position.X,
                Y = item.Position.Y,
                Type = item.GetType().ToString(),
            };

            return sObj;
        }

        public static implicit operator Item(ItemSerialized sObj) {
            Item newItem = new Item(new Color(sObj.FG), new Color(sObj.BG), sObj.Name, (char)sObj.Glyph, sObj.Weight, sObj.Condition, quantity: sObj.Quantity, plural: sObj.NamePlural, slot: sObj.Slot, properties: sObj.Properties);
            newItem.Position = new Point(sObj.X, sObj.Y);


            return newItem;
        }
    }
}