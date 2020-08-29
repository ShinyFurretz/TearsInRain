using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TearsInRain.Tiles;

namespace TearsInRain.Serializers {
    public class TileJsonConverter : JsonConverter<TileBase> {
        public override void WriteJson(JsonWriter writer, TileBase value, JsonSerializer serializer) {
            serializer.Serialize(writer, (TileSerialized)value);
        }

        public override TileBase ReadJson(JsonReader reader, Type objectType, TileBase existingValue, 
                                        bool hasExistingValue, JsonSerializer serializer) {
            return serializer.Deserialize<TileSerialized>(reader);
        }
    }

    /// <summary>
    /// Serialized instance of a <see cref="TileBase"/>.
    /// </summary>
    [DataContract]
    public class TileSerialized {
        // Visuals
        [DataMember] public uint FG; // Foreground
        [DataMember] public uint BG; // Background
        [DataMember] public int Glyph; // Glyph

        [DataMember] public string Name; // Item Name
        [DataMember] public bool blocksLOS; // Whether or not this tile blocks Line of Sight
        [DataMember] public bool blocksMove; // Whether or not this tile can be walked on

        public static implicit operator TileSerialized(TileBase tile) { 
            var sObj = new TileSerialized() {
                FG = tile.Foreground.PackedValue,
                BG = tile.Background.PackedValue,
                Glyph = tile.Glyph,
                Name = tile.Name,
                blocksLOS = tile.IsBlockingLOS,
                blocksMove = tile.IsBlockingMove,
            };

            return sObj;
        }

        public static implicit operator TileBase(TileSerialized sObj) {
            return new TileBase(new Color(sObj.FG), new Color(sObj.BG), sObj.Glyph, sObj.blocksMove, sObj.blocksLOS, sObj.Name);
        }
    }
}