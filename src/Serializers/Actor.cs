using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using Newtonsoft.Json;
using TearsInRain.Entities;
using Microsoft.Xna.Framework;
using TearsInRain.src;

namespace TearsInRain.Serializers {
    public class ActorJsonConverter : JsonConverter<Actor> {
        public override void WriteJson(JsonWriter writer, Actor value, JsonSerializer serializer) {
            serializer.Serialize(writer, (ActorSerialized)value);
        }

        public override Actor ReadJson(JsonReader reader, Type objectType, Actor existingValue,
                                        bool hasExistingValue, JsonSerializer serializer) {
            return serializer.Deserialize<ActorSerialized>(reader);
        }
    }

    /// <summary>
    /// Serialized instance of a <see cref="Entity"/>.
    /// </summary>
    [DataContract]
    public class ActorSerialized {
        // Visuals
        [DataMember] public uint FG; // Foreground
        [DataMember] public uint BG; // Background
        [DataMember] public int G; // Glyph
        [DataMember] public string tilesheetName;

        [DataMember] public string Name;
        [DataMember] public int X;
        [DataMember] public int Y;

        //Primary Stats
        [DataMember] public int STR;
        [DataMember] public int DEX;
        [DataMember] public int CON;
        [DataMember] public int INT;
        [DataMember] public int WIS;
        [DataMember] public int CHA;

        [DataMember] public int Level;
        [DataMember] public List<CharacterClass> Classes;

        // Secondary Direct
        [DataMember] public int HP; // Health
        [DataMember] public int MaxHP;

        [DataMember] public int STA; // Stamina
        [DataMember] public int MaxSTA;
        [DataMember] public int ENG; // Energy
        [DataMember] public int MaxENG;

        [DataMember] public int BASE_SPD;
        [DataMember] public int SPD; // Speed
        [DataMember] public int Dodge;

        [DataMember] public string ClassSkills;
        [DataMember] public Dictionary<string, Skill> Skills;
        [DataMember] public int RanksPerLvl;
        [DataMember] public int MiscRanksMod;

        [DataMember] public List<Item> Inventory;
        [DataMember] public Dictionary<string, SadConsole.CellDecorator> decorators;


        public static implicit operator ActorSerialized(Actor actor) {
            var serializedObject = new ActorSerialized() {
                FG = actor.Animation.CurrentFrame[0].Foreground.PackedValue,
                BG = actor.Animation.CurrentFrame[0].Background.PackedValue,
                G = actor.Animation.CurrentFrame[0].Glyph,
                tilesheetName = actor.tilesheetName,


                Name = actor.Name,
                STR = actor.Strength,
                DEX = actor.Dexterity,
                CON = actor.Constitution,
                INT = actor.Intelligence,
                WIS = actor.Wisdom,
                CHA = actor.Charisma,

                Level = actor.Level,
                Classes = actor.Classes,

                HP = actor.Health,
                MaxHP = actor.MaxHealth,
                STA = actor.CurrentStamina,
                MaxSTA = actor.MaxStamina,
                ENG = actor.CurrentEnergy,
                MaxENG = actor.MaxEnergy,

                SPD = actor.Speed,
                BASE_SPD = actor.BaseSpeed,
                Dodge = actor.Dodge,
                Inventory = actor.Inventory,

                Skills = actor.Skills,
                ClassSkills = actor.ClassSkills,
                RanksPerLvl = actor.RanksPerLvl,
                MiscRanksMod = actor.MiscRanksMod,


                X = actor.Position.X,
                Y = actor.Position.Y,

                decorators = actor.decorators,
            };

            return serializedObject;
        }

        public static implicit operator Actor(ActorSerialized serializedObject) {
            var entity = new Actor(new Color(serializedObject.FG), new Color(serializedObject.BG), serializedObject.G);
            entity.tilesheetName = serializedObject.tilesheetName;

            entity.Name = serializedObject.Name;

            entity.Strength = serializedObject.STR;
            entity.Dexterity = serializedObject.DEX;
            entity.Constitution = serializedObject.CON;
            entity.Intelligence = serializedObject.INT;
            entity.Wisdom = serializedObject.WIS;
            entity.Charisma = serializedObject.CHA;

            entity.Health = serializedObject.HP;
            entity.MaxHealth = serializedObject.MaxHP;
            entity.CurrentStamina = serializedObject.STA;
            entity.MaxStamina = serializedObject.MaxSTA;
            entity.CurrentEnergy = serializedObject.ENG;
            entity.MaxEnergy = serializedObject.MaxENG;

            entity.Speed = serializedObject.SPD;
            entity.BaseSpeed = serializedObject.BASE_SPD;
            entity.BaseDodge = serializedObject.Dodge;
            entity.Dodge = serializedObject.Dodge;

            entity.Inventory = serializedObject.Inventory;

            entity.Skills = serializedObject.Skills;
            entity.ClassSkills = serializedObject.ClassSkills;
            entity.Level = serializedObject.Level;
            entity.Classes = serializedObject.Classes;
            entity.RanksPerLvl = serializedObject.RanksPerLvl;
            entity.MiscRanksMod = serializedObject.MiscRanksMod;
            entity.decorators = serializedObject.decorators;

            entity.Position = new Point(serializedObject.X, serializedObject.Y);

            return entity;
        }
    }
}