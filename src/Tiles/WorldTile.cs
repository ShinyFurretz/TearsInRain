using Microsoft.Xna.Framework;

namespace TearsInRain.Tiles {
    public class WorldTile {
        protected int Width = 50;
        protected int Height = 50;
        

        // How the rainfall is divided between seasons. Four numbers separated by /, must equal 100
        public string RainDistribution = "25/25/25/25";

        // Average yearly rainfall measured in millimeters
        public int AverageAnnualRainfall;



        public float Savagery = 0.5f; // 0.00 - 0.33 = Benign, 0.34 - 0.66 = Neutral, 0.67 - 1.0 = Savage
        public float Vitality = 0.5f; // 0.00 - 0.33 = Lethargic, 0.34 - 0.66 = Neutral, 0.67 - 1.0 = Frenetic



        public TileBase[] tiles;
        public Point worldPos;



        public WorldTile(string RainDist, int AnnualRainfall, Point worldPosition) {
            RainDistribution = RainDist;
            AverageAnnualRainfall = AnnualRainfall;


            worldPos = worldPosition;
        }


        public WorldTile(Point position) {

        }



        public string BiomePrefix() {
            if (Savagery <= 0.33f) {
                if (Vitality <= 0.33f) {
                    return "Serene";
                } else if (Vitality >= 0.34f && Vitality <= 0.66f) {
                    return "Peaceful";
                } else {
                    return "Playful";
                }
            } else if (Savagery >= 0.34f && Savagery <= 0.66f) {
                if (Vitality <= 0.33f) {
                    return "Reserved";
                } else if (Vitality >= 0.34f && Vitality <= 0.66f) {
                    return "Calm";
                } else {
                    return "Untamed";
                }
            } else {
                if (Vitality <= 0.33f) {
                    return "Menacing";
                } else if (Vitality >= 0.34f && Vitality <= 0.66f) {
                    return "Dangerous";
                } else {
                    return "Feral";
                }
            }
        }
    }
}
