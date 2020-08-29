using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SadConsole;
using TearsInRain.Serializers;

namespace TearsInRain.Tiles {

    [JsonConverter(typeof(TileJsonConverter))]
    public class TileBase : Cell {
        public bool IsBlockingMove;
        public bool IsBlockingLOS;
        public string Name;
        public bool IsExplored = false;


        public TileBase(Color FG, Color BG, int glyph = 0, bool blockingMove = false, bool blockingLOS = false, String name = "") : base(FG, BG, glyph) {
            IsBlockingMove = blockingMove;
            IsBlockingLOS = blockingLOS;
            Name = name;
            IsVisible = true; 
        }


        public new TileBase Clone() {
            return new TileBase(Foreground, Background, Glyph, IsBlockingMove, IsBlockingLOS, Name);
        }


        public void Darken(bool isGray) {
            if (isGray) {
                Foreground.A = 200;
                Background.A = 200;
            } else {
                Foreground.A = 255;
                Background.A = 255;
            }
        }


        public void Reset() {
            IsVisible = false;
            Foreground.A = 255;
            Background.A = 255;
        }
    }
}