using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using Newtonsoft.Json;
using TearsInRain.Entities;
using Microsoft.Xna.Framework;

namespace TearsInRain.Serializers {
    public class TerrainJsonConverter : JsonConverter<TerrainFeature> {
        public override void WriteJson(JsonWriter writer, TerrainFeature value, JsonSerializer serializer) {
            serializer.Serialize(writer, (TerrainSerialized)value);
        }

        public override TerrainFeature ReadJson(JsonReader reader, Type objectType, TerrainFeature existingValue,
                                        bool hasExistingValue, JsonSerializer serializer) {
            return serializer.Deserialize<TerrainSerialized>(reader);
        }
    }

    /// <summary>
    /// Serialized instance of a <see cref="TerrainFeature"/>.
    /// </summary>
    [DataContract]
    public class TerrainSerialized {
        // Visuals
        [DataMember] public uint FG; // Foreground (main)
        [DataMember] public uint BG; // Background (main)
        [DataMember] public int G; // Glyph (main)

        [DataMember] public string Name;
        [DataMember] public int X;
        [DataMember] public int Y;

        [DataMember] public string madeBy = "";
        [DataMember] public double Weight;
        [DataMember] public int Condition;

        [DataMember] public bool IsBlockingLOS;
        [DataMember] public bool IsBlockingMove;

        [DataMember] public uint DecoColor;
        [DataMember] public int DecoGlyph;
        [DataMember] public Dictionary<string, string> Properties = new Dictionary<string, string>();

        public static implicit operator TerrainSerialized(TerrainFeature actor) {
            var serializedObject = new TerrainSerialized() {
                FG = actor.Animation.CurrentFrame[0].Foreground.PackedValue,
                BG = actor.Animation.CurrentFrame[0].Background.PackedValue,
                G = actor.Animation.CurrentFrame[0].Glyph,
                X = actor.Position.X,
                Y = actor.Position.Y,

                Name = actor.Name,

                madeBy = actor.madeBy,
                Weight = actor.Weight,
                Condition = actor.Condition,
                IsBlockingLOS = actor.IsBlockingLOS,
                IsBlockingMove = actor.IsBlockingMove,
                DecoColor = actor.DecoColor.PackedValue,
                DecoGlyph = actor.DecoGlyph,
                Properties = actor.Properties,
            };



            return serializedObject;
        }

        public static implicit operator TerrainFeature(TerrainSerialized sObj) {
            return new TerrainFeature(new Color(sObj.FG), new Color(sObj.BG), sObj.Name, (char)sObj.G, sObj.IsBlockingLOS, sObj.IsBlockingMove, sObj.Weight, sObj.Condition, 1, 1, new Color(sObj.DecoColor), (char)sObj.DecoGlyph, sObj.Properties) { Position = new Point(sObj.X, sObj.Y) };
        }
    }
}