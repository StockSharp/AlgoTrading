# AMA Trader 2 Strategy

## Overview
The AMA Trader 2 strategy replicates the averaging workflow of the original MetaTrader expert by Vladimir Karputov. It combines a Kaufman Adaptive Moving Average (AMA) trend filter with a Relative Strength Index (RSI) confirmation block. When price closes above the AMA and the RSI dips into oversold territory the strategy adds long exposure; the symmetric rule applies to short trades when price closes below the AMA while RSI prints an overbought reading. Averaging trades are submitted in fixed lot sizes and can be constrained through risk parameters such as maximum position count, minimum entry spacing, and protective trailing stops.

## Market Assumptions
- **Instrument**: Designed for FX/CFD symbols traded with tight spreads, but applicable to any liquid instrument where averaging is acceptable.
- **Data**: Operates on finished time-based candles. The timeframe is configurable through the `CandleType` parameter (default: 1 minute).
- **Sessions**: Optional intraday window. Trading can be confined to a start/end time in UTC with the `UseTimeWindow` flag.

## Indicators
1. **Kaufman Adaptive Moving Average (AMA)** – detects the prevailing trend with configurable fast/slow smoothing constants and averaging length.
2. **Relative Strength Index (RSI)** – validates momentum extremes. The number of consecutive RSI readings that must confirm a signal is controlled by `StepLength` (0 behaves like 1, matching the MQL version).

## Trading Logic
1. Process only finished candles and ensure the strategy is online and allowed to trade.
2. Apply the optional time filter; skip processing outside the intraday window when enabled.
3. Update the queue of recent RSI values and compute trailing-stop adjustments for existing exposure.
4. **Long setup**: close price above AMA and at least one of the inspected RSI values below `RsiLevelDown`. If the active long position is losing money, an averaging order is queued before the standard entry, mimicking the "loss recovery" behaviour of the expert advisor. Short signals follow the symmetric rule (`RsiLevelUp`).
5. Entries honour `MaxPositions`, `MinStep`, and `OnlyOnePosition`. When `CloseOpposite` is enabled the strategy first offsets the opposing side and only considers new entries after the flattening trade is confirmed.
6. Every new position can attach fixed stop-loss/take-profit distances and optionally enable a profit-based trailing stop with activation, distance, and step thresholds.

## Risk Management
- **Fixed lot size**: All entries use `LotSize`, allowing position sizing via the parameter or the hosting portfolio.
- **Maximum averaging depth**: `MaxPositions` limits how many times exposure can be increased per direction.
- **Spacing control**: `MinStep` enforces a minimum price distance between consecutive entries, reducing clustering at the same level.
- **Protective exits**: Optional stop-loss, take-profit, and trailing logic replicate the MetaTrader expert's protective toolkit.
- **Opposite exposure**: `CloseOpposite` forces the strategy to close shorts before opening a long (and vice versa). `OnlyOnePosition` ensures the strategy never keeps both sides simultaneously.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle data type/timeframe used for calculations. |
| `LotSize` | Volume for each market order. |
| `RsiLength` | RSI averaging period. |
| `StepLength` | Number of recent RSI readings inspected (0 → 1). |
| `RsiLevelUp` | RSI overbought threshold for short entries. |
| `RsiLevelDown` | RSI oversold threshold for long entries. |
| `AmaLength` | AMA smoothing length. |
| `AmaFastPeriod` | Fast smoothing constant for AMA. |
| `AmaSlowPeriod` | Slow smoothing constant for AMA. |
| `StopLoss` | Fixed stop distance in price units (0 disables). |
| `TakeProfit` | Fixed target distance in price units (0 disables). |
| `TrailingActivation` | Profit required to arm the trailing stop (0 disables). |
| `TrailingDistance` | Distance maintained by the trailing stop. |
| `TrailingStep` | Minimum improvement before the trailing stop is tightened. |
| `MaxPositions` | Maximum averaging entries per direction (0 disables). |
| `MinStep` | Minimum distance between consecutive entries (0 disables). |
| `CloseOpposite` | Close opposite exposure before opening a trade. |
| `OnlyOnePosition` | Block new entries whenever any position exists. |
| `UseTimeWindow` | Enable intraday start/end time filtering. |
| `StartTime` | Session start time (UTC) when the window is enabled. |
| `EndTime` | Session end time (UTC) when the window is enabled. |

## Implementation Notes
- High-level API only: candles are subscribed via `SubscribeCandles`, AMA and RSI are bound with `.Bind`, and all computations happen in the bound callback without using prohibited indicator getters.
- Position accounting mirrors the MQL expert: separate accumulators track long and short volumes/average prices to evaluate unrealized PnL for averaging decisions.
- Trailing stops reconfigure the strategy-level stop-loss distance instead of manipulating order queues directly, keeping compatibility with the StockSharp execution model.
- Signals are restricted to one execution per bar per side, reproducing the MetaTrader check that prevents duplicate entries on the same candle.

## Differences from the MetaTrader Expert
- MetaTrader-specific parameters such as magic numbers, deviation, freeze level checks, and tester withdrawal emulation are omitted. The StockSharp environment manages order slippage and fees internally.
- Stop/limit prices are calculated from the candle close rather than bid/ask ticks. This matches StockSharp's candle-based workflow.
- The original EA uses account margin settings to compute dynamic lot sizes. The port keeps a fixed `LotSize`, leaving risk-based sizing to the hosting environment.
