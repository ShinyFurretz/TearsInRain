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
    }
}
