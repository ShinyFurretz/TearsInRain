﻿using TearsInRain.Tiles;
using SadConsole;
using TearsInRain.Entities;
using System.Linq;
using Point = Microsoft.Xna.Framework.Point;

namespace TearsInRain {
    public class Map {
        TileBase[] _tiles;
        private int _width;
        private int _height;

        public TileBase[] Tiles { get { return _tiles; } set { _tiles = value; } }
        public int Width { get { return _width; } set { _width = value; } }
        public int Height { get { return _height; } set { _height = value; } }

        public GoRogue.MultiSpatialMap<Entity> Entities;
        public static GoRogue.IDGenerator IDGenerator = new GoRogue.IDGenerator();
        
        public Map(int width, int height) {
            _width = width;
            _height = height;
            Tiles = new TileBase[width * height];
            Entities = new GoRogue.MultiSpatialMap<Entity>();
        }

        public Map(TileBase[] tiles) {
            _width = 100;
            _height = 100;
            Tiles = new TileBase[100 * 100];
            Tiles = tiles;
            Entities = new GoRogue.MultiSpatialMap<Entity>();
        }


        public bool IsTileWalkable(Point location) {
            if (location.X < 0 || location.Y < 0 || location.X >= Width || location.Y >= Height)
                return false;
            return !_tiles[location.Y * Width + location.X].IsBlockingMove;
        }
        
        public T GetTileAt<T>(int x, int y) where T : TileBase {
            int locationIndex = Helpers.GetIndexFromPoint(x, y, Width);
            if (locationIndex <= Width * Height && locationIndex >= 0) {
                if (Tiles[locationIndex] is T)
                    return (T)Tiles[locationIndex];
                else return null;
            } else return null;
        }
        
        public T GetEntityAt<T>(Point location) where T : Entity {
            return Entities.GetItems(location).OfType<T>().FirstOrDefault();
        }
        
        public void Remove(Entity entity) {
            Entities.Remove(entity);
            
            entity.Moved -= OnEntityMoved;
        }
        
        public void Add(Entity entity) {
            Entities.Add(entity, entity.Position);
            
            entity.Moved += OnEntityMoved;
        }
        
        private void OnEntityMoved(object sender, Entity.EntityMovedEventArgs args) {
            Entities.Move(args.Entity as Entity, args.Entity.Position);
        }
    }
}
