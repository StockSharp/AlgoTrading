# Exp Martin V2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Exp Martin V2 strategy implements an exponential martingale approach. It always keeps a single open position and after each trade decides the next direction and volume based on the profit of the last deal.

The strategy starts with a predefined order type (buy or sell) and a starting volume. A fixed take-profit and stop-loss are applied to every position. When a trade closes with profit, a new position of the same type and starting volume is opened. If the trade ends with a loss, the direction is reversed and the volume is multiplied by a specified factor. The multiplication continues after each loss until a maximum number of multiplications is reached; then the volume resets to the starting value.

This creates an escalating sequence of opposite trades that aims to recover previous losses once a profitable move occurs.

## Details

- **Entry Logic**:
  - Open the initial position according to *Start Type* (0 - buy, 1 - sell) with the *Start Volume*.
  - After a profitable trade, repeat the same direction with the starting volume.
  - After a losing trade, reverse direction and multiply the volume by *Factor* until *Limit* multiplications are reached.
- **Long/Short**: Both, depending on current sequence.
- **Exit Logic**:
  - Positions are closed when price hits the configured *Take Profit* or *Stop Loss* levels.
- **Stops**: Fixed stop-loss and take-profit in points.
- **Filters**: None.
- **Position Management**: Only one position is open at a time.

Use this strategy to experiment with martingale money management in StockSharp without any additional indicators.
