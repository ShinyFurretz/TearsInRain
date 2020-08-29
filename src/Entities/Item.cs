using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using TearsInRain.Serializers;
using TearsInRain.Tiles;

namespace TearsInRain.Entities {

    [JsonConverter(typeof(ItemJsonConverter))]
    public class Item : Entity {
        public int Condition;
        public double Weight;
        public int Quantity;
        public string NamePlural;
        public int Slot;
        public string _id;
        public Dictionary<string, string> Properties; // First string is property name, second string is any relevant data that tag needs.

        public Item(Color foreground, Color background, string name, char glyph, double weight = 1, int condition = 100, int width = 1, int height = 1, int quantity = 1, string plural = "", int slot = -1, Dictionary<string, string> properties = null, string id = "") : base(foreground, background, glyph) {
            Animation.CurrentFrame[0].Foreground = foreground;
            Animation.CurrentFrame[0].Background = background;
            Animation.CurrentFrame[0].Glyph = glyph;
            Weight = weight;

            Condition = condition;
            Name = name;
            Slot = slot;

            if (plural == "") {
                NamePlural = name + "s";
            } else {
                NamePlural = plural;
            }

            if (id == "") {
                _id = name;
            } else {
                _id = id;
            }

            Quantity = quantity;
            Properties = properties;
        }

        public Item Clone() {
            Color fore = Animation.CurrentFrame[0].Foreground;
            Color back = Animation.CurrentFrame[0].Background;
            char glyph = (char) Animation.CurrentFrame[0].Glyph;

            Dictionary<string, string> newProps = new Dictionary<string, string>();

            foreach(KeyValuePair<string, string> prop in Properties) {
                newProps.Add(prop.Key, prop.Value);
            }


            Item newItem = new Item(fore, back, Name, glyph, Weight, Condition, quantity:Quantity, plural:NamePlural, slot:Slot, properties:newProps, id:_id);

            return newItem;
        }


        public void DamageItem(int dmg) {
            Condition -= dmg;

            if (Condition <= 0) {
                Destroy();
            }
        }


        public void Destroy() {
            GameLoop.World.CurrentMap.Remove(this);
        }


        public double StackWeight() {
            return Weight * Quantity;
        }


        public void UseItem(Player player, Point pos) {
            if ((player.Equipped[0] != null && player.Equipped[0].Properties["qualities"].Contains("tilling")) || (player.Equipped[13] != null && player.Equipped[13].Properties["qualities"].Contains("tilling"))) {
                if (GameLoop.World.CurrentMap.GetTileAt<TileBase>(pos.X, pos.Y).Name == "grass") {
                    TileBase tile = GameLoop.World.CurrentMap.GetTileAt<TileBase>(pos.X, pos.Y);

                    if (GameLoop.World.CurrentMap.GetTileAt<TileBase>(pos) != null) {
                        GameLoop.World.CurrentMap.NewTiles[pos] = GameLoop.TileLibrary["farmland"].Clone();
                        GameLoop.UIManager.RefreshMap(player.Position);

                        player.CurrentStamina -= 5;

                        string tileUpdate = "t_data|farmland|" + pos.X + "|" + pos.Y;
                        GameLoop.NetworkingManager.SendNetMessage(0, System.Text.Encoding.UTF8.GetBytes(tileUpdate));
                    }
                }
            }
        }
    }
}