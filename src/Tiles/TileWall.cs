﻿using System;
using Microsoft.Xna.Framework;

namespace TearsInRain.Tiles { 
    public class TileWall : TileBase {
        public TileWall(bool blocksMovement=true, bool blocksLOS=true) : base(Color.LightGray, Color.Transparent, '#', blocksMovement, blocksLOS) {
            Name = "Wall";
            Foreground = new Color(120, 120, 120);
            Background = new Color(100, 100, 100);
        }
    }
}