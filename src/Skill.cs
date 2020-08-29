using Newtonsoft.Json;
using System;
using TearsInRain.Entities;
using TearsInRain.Serializers;

namespace TearsInRain.src {

    [JsonConverter(typeof(SkillJsonConverter))]
    public class Skill {
        public string Name;
        public string ControllingAttribute;
        public int Ranks;


        public Skill(string name, string controllingAttrib, int ranks) {
            Name = name;
            ControllingAttribute = controllingAttrib;
            Ranks = ranks;
        }



        public int SkillCheck(Actor actor, int miscMods) {
            int abilityMod = 0;
            if (ControllingAttribute == "STR") { abilityMod = (int) Math.Floor((double) ((actor.Strength - 10) / 2));  }
            if (ControllingAttribute == "DEX") { abilityMod = (int) Math.Floor((double) ((actor.Dexterity - 10) / 2)); }
            if (ControllingAttribute == "CON") { abilityMod = (int) Math.Floor((double) ((actor.Constitution - 10) / 2)); }
            if (ControllingAttribute == "INT") { abilityMod = (int) Math.Floor((double) ((actor.Intelligence - 10) / 2)); }
            if (ControllingAttribute == "WIS") { abilityMod = (int) Math.Floor((double) ((actor.Wisdom - 10) / 2)); }
            if (ControllingAttribute == "CHA") { abilityMod = (int) Math.Floor((double) ((actor.Charisma - 10) / 2)); }

            int totalMod = abilityMod + miscMods + Ranks; 
            if (actor.ClassSkills.Contains(Name)) { totalMod += 3; }


            return (GoRogue.DiceNotation.Dice.Roll("1d20") + totalMod);
        }
    }
}
