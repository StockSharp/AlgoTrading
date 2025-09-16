# Easiest Ever Daytrade Strategy

## Overview
- Conversion of the MetaTrader 4 expert advisor **"Easiest ever - daytrade robot"** to the StockSharp high-level API.
- Designed for simple day trading: every session opens at most one market position that follows the direction of the previous daily candle.
- Uses only candle data, without technical indicators or oscillators. All order management is performed through market orders.

## Trading Logic
1. Collect daily candles (`DailyCandleType`, default `TimeSpan.FromDays(1)`) and store the last completed day's open and close prices.
2. Subscribe to intraday candles (`IntradayCandleType`, default `TimeSpan.FromMinutes(1)`) to drive execution.
3. During the early session hours (while the candle open hour is strictly less than `EntryHourLimit`, default `1`):
   - If the previous daily close is above the previous daily open, enter a long position using `BuyMarket(TradeVolume)`.
   - If the previous daily close is below the previous daily open, enter a short position using `SellMarket(TradeVolume)`.
   - If the daily candle closed flat (open equals close), no trade is opened.
4. Hold the position through the day. When the intraday candle hour is greater than or equal to `MarketCloseHour` (default `20`), close any open exposure with a market order (`SellMarket` for longs, `BuyMarket` for shorts).
5. The strategy only opens a new position when no active position exists, ensuring one trade per day at most.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `TradeVolume` | Order volume for both long and short entries. Must be positive. | `1` |
| `EntryHourLimit` | Latest hour (exclusive) when a new trade can be initiated. Values outside `[0, 23]` are clamped via validation. | `1` |
| `MarketCloseHour` | Hour when the strategy forcefully closes any open position. Applies daily. | `20` |
| `IntradayCandleType` | Timeframe used for trade execution logic and position management. | `TimeSpan.FromMinutes(1).TimeFrame()` |
| `DailyCandleType` | Timeframe used to read the previous day's open and close prices. | `TimeSpan.FromDays(1).TimeFrame()` |

All parameters are registered through `Param()` and can be optimized in the StockSharp optimizer.

## Risk Management
- The strategy does not use stop-loss or take-profit levels; risk is controlled by the daily exit at `MarketCloseHour`.
- `StartProtection()` is enabled on start to guard against unexpected non-flat positions during trading.
- Because only one position can be active per day, the maximum exposure is defined by `TradeVolume`.

## Usage Notes
- Run the strategy on instruments that provide both intraday and daily candle histories. The default configuration requires minute and daily candles.
- Align the `EntryHourLimit` and `MarketCloseHour` with the trading session of the selected instrument.
- The algorithm expects exchange-local time in the candle timestamps; adjust data sources accordingly.
- The logic mirrors the original MQL expert advisor, allowing the behaviour to be replicated inside the StockSharp environment without Python components.
