# Trend Follower Rainbow Strategy

## Overview
Trend Follower Rainbow Strategy is a C# port of the MetaTrader 4 expert advisor "TrendFollowerRainbowMethodkyast773". The strategy combines several confirmation layers to trade in the direction of strong trends while filtering out range-bound periods. It relies on the alignment of a rainbow of exponential moving averages, MACD momentum, Laguerre oscillator thresholds, Money Flow Index readings, and a fast/slow EMA crossover to trigger positions.

## Trading Logic
1. **Trading Window** – Signals are evaluated only when the current candle close time is strictly between the configurable start and end hours. This mimics the original EA's time filter that avoided the first and last trading hours of the session.
2. **EMA Crossover Trigger** – A long setup requires the fast EMA (default length 4) to cross above the slow EMA (default length 8). A short setup requires the opposite crossover.
3. **MACD Confirmation** – The MACD line and signal line (default 5/35/5) must both be above zero for long trades or below zero for short trades to confirm momentum alignment.
4. **Laguerre Filter** – The Laguerre filter value must cross above 0.15 for long trades or below 0.75 for short trades, reproducing the original threshold checks performed on the custom indicator.
5. **Rainbow Alignment** – Five bundles of exponential moving averages (four EMAs per bundle) must be sorted monotonically to confirm the rainbow structure. Bundles are evaluated for non-increasing order in bullish scenarios and non-decreasing order in bearish scenarios.
6. **Money Flow Index Filter** – The Money Flow Index (default period 14) must be below 40 for long entries and above 60 for short entries to avoid trading against volume-driven flow.
7. **Position Management** – Market orders are used. When an opposite signal appears, existing exposure is closed and a new position is opened in the opposite direction.

## Risk Management
The strategy supports built-in protections through StockSharp's `StartProtection` helper:
- **Take Profit** and **Stop Loss** distances are expressed in price steps to mirror the EA's point-based configuration.
- **Trailing Stop** distance also uses price steps and is activated once the protection block is started.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Base market order volume. | 1 |
| `TakeProfitPoints` | Take profit distance in price steps. | 17 |
| `StopLossPoints` | Stop loss distance in price steps. | 30 |
| `TrailingStopPoints` | Trailing stop distance in price steps. | 45 |
| `TradingStartHour` | First hour (inclusive) that is skipped before evaluating signals. | 1 |
| `TradingEndHour` | Last hour (inclusive) that is skipped after evaluating signals. | 23 |
| `FastEmaLength` | Length of the fast EMA used in the crossover trigger. | 4 |
| `SlowEmaLength` | Length of the slow EMA used in the crossover trigger. | 8 |
| `MacdFastLength` | MACD fast EMA length. | 5 |
| `MacdSlowLength` | MACD slow EMA length. | 35 |
| `MacdSignalLength` | MACD signal EMA length. | 5 |
| `LaguerreGamma` | Laguerre filter smoothing factor. | 0.7 |
| `LaguerreBuyThreshold` | Laguerre threshold crossed upward for long trades. | 0.15 |
| `LaguerreSellThreshold` | Laguerre threshold crossed downward for short trades. | 0.75 |
| `MfiPeriod` | Money Flow Index calculation period. | 14 |
| `MfiBuyLevel` | Maximum MFI level that still allows long entries. | 40 |
| `MfiSellLevel` | Minimum MFI level that still allows short entries. | 60 |
| `RainbowGroup{1..5}Base` | Base EMA length for each rainbow bundle. Four consecutive EMAs are created from each base value by adding offsets (0, 2, 4, 6). | 5 / 13 / 21 / 34 / 55 |
| `CandleType` | Primary candle series used by the strategy. Defaults to 5-minute candles. | 5-minute time frame |

## Charting
The strategy automatically draws:
- Price candles for the subscribed series.
- Fast and slow EMAs for visual confirmation of crossovers.
- Laguerre filter values to observe threshold crossings.
- Own trades plotted on the chart area.

## Notes
- The rainbow logic approximates the original RainbowMMA custom indicators by building configurable EMA bundles. Adjust the base lengths to match a specific rainbow template if needed.
- All code comments, logs, and documentation are provided in English as required.
- The strategy focuses solely on the C# implementation. No Python port is generated in this task.
