# Volume Trader Strategy

## Overview
- Port of the MetaTrader 5 expert advisor **"Volume trader" (ID 21050)** by Vladimir Karputov.
- Recreated on top of the StockSharp high level strategy API.
- Trades in the direction of the latest tick volume change while a custom trading session filter is active.

## Trading logic
1. Subscribes to candles defined by `CandleType` (default: 1-hour time frame) and reads their tick volume (`TotalVolume`).
2. On every finished candle the strategy compares the volumes of the **two previous** closed candles, mimicking the MQL5 script that runs at the birth of a new bar.
3. If the more recent volume is higher than the one before it and there is no long position, the strategy buys `Volume` contracts and additionally covers an existing short position.
4. If the more recent volume is lower than the one before it and there is no short position, the strategy sells `Volume` contracts and additionally closes an existing long position.
5. Trading signals are ignored when the opening time of the next bar falls outside the `[StartHour, EndHour]` window. The default range 09:00–18:00 replicates the original inputs.
6. No stop loss or take profit is defined by default; the strategy simply reverses on the opposite signal.

## Order management
- Entry orders are sent via `BuyMarket` or `SellMarket` to flip the position immediately at the start of a new candle.
- When a reversal signal appears, the strategy automatically trades the absolute position size plus the configured `Volume`, ensuring the previous position is closed before a new one opens.
- There is no built-in position sizing logic besides the fixed `Volume` parameter.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 1-hour time frame | Candle series used to calculate tick volume. Adjust to match the timeframe used in the original expert. |
| `StartHour` | 9 | Inclusive hour (0–23) that marks the beginning of the trading session. Signals before this hour are ignored. |
| `EndHour` | 18 | Inclusive hour (0–23) that marks the end of the trading session. Signals after this hour are ignored. |
| `Volume` | 0.1 | Order volume for new entries. Also used when flipping an existing position. |

## Usage notes
- Ensure that the data source provides tick volume in the candle messages. When only real traded volume is available, the behaviour will follow that data instead.
- Align the `CandleType` parameter with the chart timeframe you intend to reproduce from MetaTrader.
- Consider wrapping the strategy with external risk management (stop loss, take profit, daily loss limits) if required by your trading rules.
- The strategy calls `LogInfo` when a position is opened, making it easier to audit signal decisions in the log.

## Differences vs. original MQL implementation
- Uses StockSharp's candle subscription pipeline instead of manually calling `CopyTickVolume`.
- Session filtering relies on the `CloseTime` of the finished candle (the start time of the next bar) to stay aligned with the MQL logic that executes at bar opening.
- Order execution is handled through high level API helpers (`BuyMarket`, `SellMarket`) rather than direct `CTrade` calls.
