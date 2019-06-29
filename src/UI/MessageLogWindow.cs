using System;
using System.Collections.Generic;
using SadConsole;
using Microsoft.Xna.Framework;

namespace TearsInRain.UI {
    public class MessageLogWindow : Window {
        private static readonly int _maxLines = 100;
        private readonly Queue<string> _lines;
        

        private SadConsole.ScrollingConsole _messageConsole;
        private SadConsole.Controls.ScrollBar _messageScrollBar;
        private int _scrollBarCurrentPosition;
        private int _windowBorderThickness = 3;

        public MessageLogWindow(int width, int height, string title) : base(width, height) {

            _lines = new Queue<string>();
            CanDrag = true;
            Title = title.Align(HorizontalAlignment.Center, Width, (char) 205);

            _messageConsole = new SadConsole.ScrollingConsole(width - _windowBorderThickness, _maxLines);
            _messageConsole.Position = new Point(1, 1);
            _messageConsole.ViewPort = new Rectangle(0, 0, width - 1, height - _windowBorderThickness);
            _messageConsole.Cursor.DisableWordBreak = false;

            _messageScrollBar = new SadConsole.Controls.ScrollBar(SadConsole.Orientation.Vertical, height - _windowBorderThickness);
            _messageScrollBar.Position = new Point(_messageConsole.Width + 1, _messageConsole.Position.X);
            _messageScrollBar.IsEnabled = false;
            _messageScrollBar.ValueChanged += MessageScrollBar_ValueChanged;
            Add(_messageScrollBar);

            UseMouse = true;

            Children.Add(_messageConsole);
        }

        void MessageScrollBar_ValueChanged(object sender, EventArgs e) {
            _messageConsole.ViewPort = new Rectangle(0, _messageScrollBar.Value + _windowBorderThickness, _messageConsole.Width, _messageConsole.ViewPort.Height);
        }

        public override void Draw(TimeSpan drawTime) {
            base.Draw(drawTime);
        }

        public override void Update(TimeSpan time) {
            base.Update(time);

            if (_messageConsole.TimesShiftedUp != 0 | _messageConsole.Cursor.Position.Y >= _messageConsole.ViewPort.Height + _scrollBarCurrentPosition) {
                _messageScrollBar.IsEnabled = true;

                if (_scrollBarCurrentPosition < _messageConsole.Height - _messageConsole.ViewPort.Height) {
                    _scrollBarCurrentPosition += _messageConsole.TimesShiftedUp != 0 ? _messageConsole.TimesShiftedUp : 1;
                }

                _messageScrollBar.Maximum = _scrollBarCurrentPosition - _windowBorderThickness;

                _messageScrollBar.Value = _scrollBarCurrentPosition;

                _messageConsole.TimesShiftedUp = 0;
            }
        }

        public void Add(string message, Color? fgColor = null, Color? bgColor = null) {
            string[] splitMsg = new string[(int) Math.Ceiling((double) message.Length / 60)];

            if (splitMsg.Length > 1) {
                for(int i = 0; i < splitMsg.Length; i++) {
                    if (i == splitMsg.Length-1) {
                        splitMsg[i] = message.Substring(i * 10);
                    } else {
                        splitMsg[i] = message.Substring(i * 10, 60);
                    }
                }
            } else {
                splitMsg[0] = message;
            }

            for (int i = 0; i < splitMsg.Length; i++) {
                _lines.Enqueue(splitMsg[i]);

                if (_lines.Count > _maxLines) {
                    _lines.Dequeue();
                }

                _messageConsole.Cursor.Position = new Point(1, _lines.Count);

                if (fgColor == null && bgColor == null) {
                    _messageConsole.Cursor.Print(message + "\n");
                } else {

                    if (fgColor == null) { fgColor = (Color) Color.White; }
                    if (bgColor == null) { bgColor = (Color) Color.TransparentBlack; }

                    ColoredString msg = new ColoredString(message + "\n", new Cell((Color) fgColor, (Color) bgColor));

                    _messageConsole.Cursor.Print(msg);
                }
            }
        } 
    }
}
