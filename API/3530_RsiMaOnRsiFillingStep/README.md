# RSI MA on RSI Filling Step Strategy

## Overview
The **RSI MA on RSI Filling Step Strategy** is a StockSharp port of the MetaTrader expert advisor `RSI_MAonRSI_Filling Step EA.mq5`. The original system measures momentum with a Relative Strength Index (RSI) and smooths that oscillator with a moving average. Trades are initiated when RSI crosses its moving average while both values remain on the same side of the middle 50 level. The conversion keeps the configurable trade direction filters, optional session timer and the ability to reverse the signals while leveraging StockSharp's high-level indicator bindings.

## Trading Logic
1. Subscribe to the selected candle series and calculate two indicators on every finished bar: `RelativeStrengthIndex` with length `RsiPeriod` and `MovingAverage` (`MaType`, `MaPeriod`) applied to the RSI stream.
2. Wait for complete candles before acting, replicating the EA's "new bar" safeguard so that each bar produces at most one trading decision.
3. A **bullish** setup occurs when the previous RSI value was below its moving average and the latest value closes above the average while both readings stay below `MiddleLevel` (default 50). A **bearish** setup is the mirrored case above the middle level.
4. When `ReverseSignals` is enabled the bullish condition generates a short trade and the bearish condition generates a long trade, mimicking the EA's reverse flag.
5. The `Mode` parameter limits trading to long-only, short-only or both directions. Additional guards optionally close opposite exposure and block new entries when a position is already open.
6. A daily time window identical to the MetaTrader implementation can disable signals outside the configured `SessionStart` → `SessionEnd` interval, including sessions that wrap across midnight.

## Parameters
- **CandleType** – data series processed by the strategy. The default is one-hour time-frame candles.
- **RsiPeriod** – RSI averaging length (default 14).
- **MaPeriod** – length of the moving average applied to RSI (default 21).
- **MaType** – moving average type used for the RSI smoothing (default `Simple`).
- **MiddleLevel** – central RSI level used by both indicators to validate trades (default 50).
- **ReverseSignals** – flips the interpretation of the bullish/bearish crossing (default `false`).
- **Mode** – trade direction filter (`BuyOnly`, `SellOnly`, `Both`).
- **CloseOppositePositions** – whether to flatten the opposite position before entering a new trade (default `false`).
- **OnlyOnePosition** – prevents new orders while a position is already open (default `false`).
- **UseTimeWindow** – enables the daily trading session filter (default `false`).
- **SessionStart / SessionEnd** – start and end times of the allowed trading session. Works with overnight sessions by wrapping past midnight.

## Implementation Notes
- Indicator values are delivered through `Bind`, removing the need for manual buffer management that the original EA required with `CopyBuffer`.
- Previous RSI and moving-average values are cached to mirror the `RSI[m_bar_current+1]` access pattern from MQL. The `_lastSignalBarTime` field guarantees only one trade per bar, just like the EA's `m_last_deal_buy_in` / `m_last_deal_sell_in` timestamps.
- Trade management uses `BuyMarket()` and `SellMarket()` to mirror the EA's immediate market execution. Optional closing of opposite exposure is handled with `ClosePosition()` before placing the new order.
- The time filter replicates the EA's `TimeControlHourMinute` function, including the overnight window logic where the start time is greater than the end time.
- Charting helpers draw price candles with trade markers plus a dedicated RSI panel so the crossovers can be visually inspected during backtests.

## Differences Compared with the Expert Advisor
- Money-management options (`ENUM_LOT_OR_RISK`, trailing stops, freeze-level checks) are not reproduced. StockSharp users can attach their own protective logic or risk modules.
- Trade confirmations, magic number checks and manual order queues from the EA are unnecessary because StockSharp manages order lifecycles differently. The strategy assumes immediate availability of market orders.
- Stop-loss and take-profit orders are not automatically attached. Use `StartProtection` or external modules if that behaviour is required.

## Usage Tips
1. Keep `MiddleLevel` close to 50 to stay faithful to the original mean-reversion behaviour. Deviating from this value pushes the system toward breakout trading.
2. Enable `OnlyOnePosition` if you prefer strict flat-to-position transitions. Disable it to allow pyramiding with custom volume logic.
3. Combine the time filter with exchange trading hours when running on futures or stocks to avoid overnight noise.
4. Optimise `MaPeriod`, `RsiPeriod` and `MiddleLevel` together when adapting the strategy to new instruments.

With these notes you can confidently run, customise and extend the RSI MA on RSI Filling Step strategy inside the StockSharp environment.
