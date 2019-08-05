using Microsoft.Xna.Framework;

namespace TearsInRain.Tiles {
    public class Biome {
        public string Name; // Biome Name
        
        public int Humidity; // 0: Super Arid, 1: Arid, 2: Semi-Arid, 3: Dry, 4: Seasonal, 5: Humid, 6: Wet, 7: Inundated
        public int AverageTemperature; // Used to spawn the biome at the right positions. Celsius


        public TileBase baseTerrain;
        public Color baseTerrainColor;
    }
}
