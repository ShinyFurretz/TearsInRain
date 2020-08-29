using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TearsInRain.Entities;
using TearsInRain.Tiles;

namespace TearsInRain {
    public class MapGenerator {
        Random randNum = new Random();

        Map _map;
        public MapGenerator() {
        }




        public TileBase GenerateTile(float noise) {
            if (noise < 128) {
                return GameLoop.TileLibrary["grass"];
            } else {
                return GameLoop.TileLibrary["wall"];
            }
        }








        public Map GenerateMap(int mapWidth, int mapHeight, int maxRooms, int minRoomSize, int maxRoomSize) {
            _map = new Map(mapWidth, mapHeight);
            

            return _map;
        }
        
        private List<Point> GetBorderCellLocations(Rectangle room) {
            int xMin = room.Left;
            int xMax = room.Right;
            int yMin = room.Top;
            int yMax = room.Bottom;
            
            List<Point> borderCells = GetTileLocationsAlongLine(xMin, yMin, xMax, yMin).ToList();
            borderCells.AddRange(GetTileLocationsAlongLine(xMin, yMin, xMin, yMax));
            borderCells.AddRange(GetTileLocationsAlongLine(xMin, yMax, xMax, yMax));
            borderCells.AddRange(GetTileLocationsAlongLine(xMax, yMin, xMax, yMax));

            return borderCells;
        }
        
        
        
        public IEnumerable<Point> GetTileLocationsAlongLine(int xOrigin, int yOrigin, int xDestination, int yDestination) {
            
            xOrigin = ClampX(xOrigin);
            yOrigin = ClampY(yOrigin);
            xDestination = ClampX(xDestination);
            yDestination = ClampY(yDestination);

            int dx = Math.Abs(xDestination - xOrigin);
            int dy = Math.Abs(yDestination - yOrigin);

            int sx = xOrigin < xDestination ? 1 : -1;
            int sy = yOrigin < yDestination ? 1 : -1;
            int err = dx - dy;

            while (true) {

                yield return new Point(xOrigin, yOrigin);
                if (xOrigin == xDestination && yOrigin == yDestination) {
                    break;
                }
                int e2 = 2 * err;
                if (e2 > -dy) {
                    err = err - dy;
                    xOrigin = xOrigin + sx;
                }
                if (e2 < dx) {
                    err = err + dx;
                    yOrigin = yOrigin + sy;
                }
            }
        }

        
        private int ClampX(int x) {
            if (x < 0)
                x = 0;
            else if (x > _map.Width - 1)
                x = _map.Width - 1;
            return x;
        }
        
        private int ClampY(int y) {
            if (y < 0)
                y = 0;
            else if (y > _map.Height - 1)
                y = _map.Height - 1;
            return y;
        }
    }
}
