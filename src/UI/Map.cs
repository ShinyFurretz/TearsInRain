using TearsInRain.Tiles;
using SadConsole;
using TearsInRain.Entities;
using System.Linq;
using Point = Microsoft.Xna.Framework.Point;
using GoRogue.MapViews;
using GoRogue;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TearsInRain.Serializers;
using Newtonsoft.Json;

namespace TearsInRain {

    [JsonConverter(typeof(MapJsonConverter))]
    public class Map{
        //TileBase[] _tiles;
        public int Width { get; }
        public int Height { get; }

        //public TileBase[] Tiles;

        public Dictionary<Point, TileBase> NewTiles = new Dictionary<Point, TileBase>();

        public GoRogue.MultiSpatialMap<Entity> Entities;
        public static GoRogue.IDGenerator IDGenerator = new GoRogue.IDGenerator();
        
        public Map(int width, int height) {
            Width = width;
            Height = height;

            if (GameLoop.ReceivedEntities != null) {
                Entities = GameLoop.ReceivedEntities;
            } else {
                Entities = new GoRogue.MultiSpatialMap<Entity>();
            }
        }

        public Map(Dictionary<Point, TileBase> tiles, int W = 100, int H = 100) {
            Width = W;
            Height = H;

            foreach (KeyValuePair<Point, TileBase> tile in tiles) {
                NewTiles.Add(tile.Key, tile.Value);
            }

            if (GameLoop.ReceivedEntities != null) {
                Entities = GameLoop.ReceivedEntities;
            } else {
                Entities = new GoRogue.MultiSpatialMap<Entity>();
            }
        }


        public bool IsTileWalkable(Point location) {
            TerrainFeature terrain = GetEntityAt<TerrainFeature>(location);

            if (terrain != null) {
                return !GetTileAt<TileBase>(location).IsBlockingMove && !terrain.IsBlockingMove;
            }

            return !GetTileAt<TileBase>(location).IsBlockingMove;
        }

        public T GetTileAt<T>(int x, int y) where T : TileBase {
            Point loc = new Point(x, y);

            if (NewTiles.ContainsKey(loc) && NewTiles[loc] is T) {
                if (NewTiles[loc].literalPos != loc) {
                    NewTiles[loc].literalPos = loc;
                }

                return (T)NewTiles[loc];
            } else if (!NewTiles.ContainsKey(loc)) {
                NewTiles.Add(loc, GenerateTile(SimplexNoise.Noise.CalcPixel2D(loc.X, loc.Y, 0.5f)));

                NewTiles[loc].literalPos = loc;

                return (T)NewTiles[loc];
            } else return null;
        }


        public T GetTileAt<T>(Point loc) where T : TileBase {
            if (NewTiles.ContainsKey(loc) && NewTiles[loc] is T) {
                if (NewTiles[loc].literalPos != loc) {
                    NewTiles[loc].literalPos = loc;
                }


                
                return (T) NewTiles[loc];
            } else if (!NewTiles.ContainsKey(loc)) {
                NewTiles.Add(loc, GenerateTile(SimplexNoise.Noise.CalcPixel2D(loc.X, loc.Y, 0.5f)));
                NewTiles[loc].literalPos = loc;

                if (!GameLoop.IsHosting) {
                    string msg = "t_request|" + loc.X + "|" + loc.Y;
                    GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(msg));
                }


                return (T) NewTiles[loc];
            } else return null;
        }

        public TileBase[] GetTileRegion(Point center) {
            int startY = center.Y - (GameLoop.UIManager.MapConsole.Height / 2);
            int startX = center.X - (GameLoop.UIManager.MapConsole.Width / 2);
            

            TileBase[] region = new TileBase[GameLoop.UIManager.MapConsole.Height * GameLoop.UIManager.MapConsole.Width];
            
            for (int y = 0; y < (GameLoop.UIManager.MapConsole.Height); y++) {
                for (int x = 0; x < (GameLoop.UIManager.MapConsole.Width); x++) {
                    region[new Point(x, y).ToIndex(GameLoop.UIManager.MapConsole.Width)] = GetTileAt<TileBase>(x + startX, y + startY);
                }
            }
            return region;
        }
        
        public T GetEntityAt<T>(Point location) where T : Entity {
            return Entities.GetItems(location).OfType<T>().FirstOrDefault();
        }

        public List<T> GetEntitiesAt<T>(Point location) where T : Entity {
            return Entities.GetItems(location).OfType<T>().ToList<T>();
        }

        public void Remove(Entity entity) {
            Entities.Remove(entity);
            
            entity.Moved -= OnEntityMoved;
        }
        
        public void Add(Entity entity) {
            Entities.Add(entity, entity.literalPosition);
            
            entity.Moved += OnEntityMoved;
        }
        
        private void OnEntityMoved(object sender, Entity.EntityMovedEventArgs args) {
            int posIndex = args.Entity.Position.ToIndex(GameLoop.UIManager.MapConsole.Width);
            
            if (GameLoop.UIManager.latestRegion != null && GameLoop.UIManager.latestRegion.Length > posIndex && posIndex >= 0) {
                Point literalPos = GameLoop.UIManager.latestRegion[args.Entity.Position.ToIndex(GameLoop.UIManager.MapConsole.Width)].literalPos;

                if (args.Entity is Entity entity) {
                    entity.literalPosition = literalPos;
                    Entities.Move(entity, entity.literalPosition);
                }
            }

            if (GameLoop.World.players.ContainsKey(GameLoop.NetworkingManager.myUID)) {
                GameLoop.UIManager.CenterOnActor(GameLoop.World.players[GameLoop.NetworkingManager.myUID], false);
            }


        }

        public void SetTile(Point pos, TileBase tile) {
            if (NewTiles.ContainsKey(pos))
                NewTiles[pos] = tile;
        }

        public void SetItem(Point pos, Item item) {
            
        }


        public void PlaceTrees(int num) {
            TerrainFeature tree = new TerrainFeature(Color.SaddleBrown, Color.Transparent, "tree", (char)272, true, true, 1000, 100, 2, 2, Color.Green, (char)273, null);

            for (int i = 0; i < num; i++) {
                TerrainFeature treeCopy = tree.Clone();

                treeCopy.literalPosition = (GameLoop.Random.Next(0, Width * Height)).ToPoint(Width);

                if (GetEntityAt<TerrainFeature>(treeCopy.literalPosition) == null && GetTileAt<TileBase>(treeCopy.literalPosition.X, treeCopy.literalPosition.Y).Name == "grass")
                   Add(treeCopy);
            }
        }


        public void PlaceFlowers(int num) {
            for (int i = 0; i < num; i++) {
                int flowerType = GameLoop.Random.Next(0, 5);

                switch (flowerType) {
                    case 0:
                        TerrainFeature cornflower = new TerrainFeature(Color.CornflowerBlue, Color.Transparent, "cornflower", (char)266, false, false, 0.01, 100, 1, 1, Color.Green, (char) 282);
                        cornflower.literalPosition = (GameLoop.Random.Next(0, Width * Height)).ToPoint(Width);
                        if (GetEntityAt<TerrainFeature>(cornflower.literalPosition) == null && GetTileAt<TileBase>(cornflower.literalPosition.X, cornflower.literalPosition.Y).Name == "grass")
                            Add(cornflower);
                        break;
                    case 1:
                        TerrainFeature rose = new TerrainFeature(Color.Red, Color.Transparent, "rose", (char)268, false, false, 0.01, 100, 1, 1, Color.Green, (char)284);
                        rose.literalPosition = (GameLoop.Random.Next(0, Width * Height)).ToPoint(Width);
                        if (GetEntityAt<TerrainFeature>(rose.literalPosition) == null && GetTileAt<TileBase>(rose.literalPosition.X, rose.literalPosition.Y).Name == "grass")
                            Add(rose);
                        break;
                    case 2:
                        TerrainFeature violet = new TerrainFeature(Color.Purple, Color.Transparent, "violet", (char)268, false, false, 0.01, 100, 1, 1, Color.Green, (char)284);
                        violet.literalPosition = (GameLoop.Random.Next(0, Width * Height)).ToPoint(Width);
                        if (GetEntityAt<TerrainFeature>(violet.literalPosition) == null && GetTileAt<TileBase>(violet.literalPosition.X, violet.literalPosition.Y).Name == "grass")
                            Add(violet);
                        break;
                    case 3:
                        TerrainFeature dandelion = new TerrainFeature(Color.Yellow, Color.Transparent, "dandelion", (char)267, false, false, 0.01, 100, 1, 1, Color.Green, (char)283);
                        dandelion.literalPosition = (GameLoop.Random.Next(0, Width * Height)).ToPoint(Width);
                        if (GetEntityAt<TerrainFeature>(dandelion.literalPosition) == null && GetTileAt<TileBase>(dandelion.literalPosition.X, dandelion.literalPosition.Y).Name == "grass")
                            Add(dandelion);
                        break;
                    default:
                        TerrainFeature tulip = new TerrainFeature(Color.HotPink, Color.Transparent, "tulip", (char)266, false, false, 0.01, 100, 1, 1, Color.Green, (char)282);
                        tulip.literalPosition = (GameLoop.Random.Next(0, Width * Height)).ToPoint(Width);
                        if (GetEntityAt<TerrainFeature>(tulip.literalPosition) == null && GetTileAt<TileBase>(tulip.literalPosition.X, tulip.literalPosition.Y).Name == "grass")
                            Add(tulip);
                        break;
                }
            }
        }


        public bool IsTransparent (Coord pos) {
            Point position = new Point(pos.X, pos.Y);
            position += GameLoop.UIManager.CameraOrigin;


            TerrainFeature terrain = GetEntityAt<TerrainFeature>(position);

            if (terrain != null) {
                return !GetTileAt<TileBase>(position).IsBlockingLOS && !terrain.IsBlockingLOS;
            }

            return !GetTileAt<TileBase>(position).IsBlockingLOS;
        }

        public TileBase GenerateTile(float noise) {
            if (noise < 128) {
                return GameLoop.TileLibrary["grass"].Clone();
            } else {
                return GameLoop.TileLibrary["wood floor"].Clone();
            }
        }
    }
}
