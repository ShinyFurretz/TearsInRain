using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using TearsInRain.Tiles;

namespace TearsInRain {
    class Utils {
        public static Dictionary<string, Point> Directions = new Dictionary<string, Point>();


        public static void InitDirections() {
            Directions.Add("UR", new Point(1, -1)); // Num9
            Directions.Add("U", new Point(0, -1)); // Num8, UpArrow
            Directions.Add("UL", new Point(-1, -1)); // Num7
            Directions.Add("R", new Point(1, 0)); // Num6
            Directions.Add("C", new Point(0, 0));
            Directions.Add("L", new Point(-1, 0)); // Num4
            Directions.Add("DR", new Point(1, 1)); // Num3
            Directions.Add("D", new Point(0, 1)); // Num2
            Directions.Add("DL", new Point(-1, 1)); // Num1
        }

        
       public static bool PointInArea(Point start, Point end, Point Target) {
            int smallX = Math.Min(start.X, end.X);
            int smallY = Math.Min(start.Y, end.Y);
            int largeX = Math.Max(start.X, end.X);
            int largeY = Math.Max(start.Y, end.Y);

            int width = largeX - smallX;
            int height = largeY - smallY;

            if (width == 0) { width = 1; }
            if (height == 0) { height = 1; } 

            return new Rectangle(smallX, smallY, width, height).Contains(Target);
        }



        public static string FirstCharToUpper(string s) {
            if (string.IsNullOrEmpty(s)) {
                return string.Empty;
            }

            return char.ToUpper(s[0]) + s.Substring(1);
        }
        
    }
}
