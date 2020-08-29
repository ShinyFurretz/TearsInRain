using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SadConsole;
using SadConsole.Components;
using TearsInRain.Serializers;
using TearsInRain.UI;

namespace TearsInRain.Entities {

    [JsonConverter(typeof(EntityJsonConverter))]
    public class Entity : SadConsole.Entities.Entity, GoRogue.IHasID { 
        public uint ID { get; private set; }
        public string tilesheetName;
        

        public Entity(Color foreground, Color background, int glyph, int width = 1, int height = 1) : base(width, height) {
            Animation.CurrentFrame[0].Foreground = foreground;
            Animation.CurrentFrame[0].Background = background;
            Animation.CurrentFrame[0].Glyph = glyph;


            if (GameLoop.UIManager.hold != null) {
                Font = GameLoop.UIManager.hold;
            }



            Position = Position;
            IsDirty = true;
            
            GameLoop.UIManager.IsDirty = true;

            ID = Map.IDGenerator.UseID();
            Components.Add(new EntityViewSyncComponent());


            IsVisible = false;
        }


        public void UpdateFontSize(SadConsole.Font.FontSizes newSize) {
            if (tilesheetName != null && SadConsole.Global.Fonts.ContainsKey(tilesheetName)) {
                Font = SadConsole.Global.Fonts[tilesheetName].GetFont(newSize);
            } else {
                Font = SadConsole.Global.Fonts["Cheepicus48"].GetFont(newSize);
            }
        }
    }
}