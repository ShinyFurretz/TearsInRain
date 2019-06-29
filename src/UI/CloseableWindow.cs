using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Input;
using System;
using Console = SadConsole.Console;

namespace TearsInRain.UI {
    public class CloseableWindow : Window {
        public Console console;

        public CloseableWindow(int width, int height, string title) : base(width, height) {
            CanDrag = true;
            Title = title.Align(HorizontalAlignment.Center, Width - 2, (char) 205); 
            UseMouse = true;
            Position = new Point(GameLoop.GameWidth / 2 - width / 2, GameLoop.GameHeight / 2 - height / 2);
            
            
            console = new Console(width - 2, height - 2, GameLoop.RegularSize);
            console.Position = new Point(1, 1);
            Children.Add(console);
            
            FocusOnMouseClick = true;
            SetVisibility(false);
        }


        public override void Draw(TimeSpan drawTime) {
            base.Draw(drawTime);
        }

        public override void Update(TimeSpan time) {
            base.Update(time);

            if (Global.MouseState.IsMouseOverConsole(this)) {
                if (Global.MouseState.RightClicked) {
                    SetVisibility(false);
                }
            }
        }
        
        public void SetVisibility(bool newVis) {
            IsVisible = newVis;
            console.IsVisible = newVis;
        }
    }
}
