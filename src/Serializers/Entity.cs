using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using Newtonsoft.Json;
using TearsInRain.Entities;
using Microsoft.Xna.Framework;

namespace TearsInRain.Serializers {
    public class EntityJsonConverter : JsonConverter<Entity> {
        public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer) {
            serializer.Serialize(writer, (EntitySerialized)value);
        }

        public override Entity ReadJson(JsonReader reader, Type objectType, Entity existingValue,
                                        bool hasExistingValue, JsonSerializer serializer) {
            return serializer.Deserialize<EntitySerialized>(reader);
        }
    }

    /// <summary>
    /// Serialized instance of a <see cref="Entity"/>.
    /// </summary>
    [DataContract]
    public class EntitySerialized {
        // Visuals
        [DataMember] public uint FG; // Foreground
        [DataMember] public uint BG; // Background
        [DataMember] public int G; // Glyph

        [DataMember] public string Name;
        [DataMember] public int X;
        [DataMember] public int Y;
        [DataMember] public string Type;
        
        public static implicit operator EntitySerialized(Entity actor) {
            var serializedObject = new EntitySerialized() {
                FG = actor.Animation.CurrentFrame[0].Foreground.PackedValue,
                BG = actor.Animation.CurrentFrame[0].Background.PackedValue,
                G = actor.Animation.CurrentFrame[0].Glyph,
                X = actor.Position.X,
                Y = actor.Position.Y,

                Name = actor.Name,
                Type = actor.GetType().ToString(),
            };



            return serializedObject;
        }

        public static implicit operator Entity(EntitySerialized sObj) {
            Entity entity = new Entity(new Color(sObj.FG), new Color(sObj.BG), sObj.G);


            entity.Name = sObj.Name;
            entity.Position = new Point(sObj.X, sObj.Y);

            return entity;
        }
    }
}