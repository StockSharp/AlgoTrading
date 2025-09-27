# LANZ Strategy 2.0 [Backtest]

Swing-based strategy that records market structure and trend using recent swing highs and lows.
Positions are opened at 02:00 New York time after a break of structure in the direction of the trend.
Stop-loss is set from swing points or full coverage and take-profit is calculated via risk reward multiplier.
Any open position is closed manually at 11:45 New York time.
