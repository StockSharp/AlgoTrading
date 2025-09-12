# Professional ORB Strategy

Implements an Opening Range Breakout strategy. The high and low between 09:15 and a configurable duration form the range. After the range is completed and wide enough, breakouts above or below trigger long or short entries. Positions use an ATR-based stop-loss, a fixed profit target in points, and are closed at session end. The number of trades per day is limited.
