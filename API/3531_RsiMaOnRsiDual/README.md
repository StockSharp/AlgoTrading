# RSI MA on RSI Dual Strategy

## Overview

The RSI MA on RSI Dual strategy recreates the MetaTrader expert advisor "RSI_MAonRSI_Dual" inside StockSharp. It watches two relative strength indexes with different lookback periods and applies a common moving average on top of each RSI stream. Trading decisions are taken when the smoothed RSI lines cross one another while remaining on the same side of a configurable neutral level.

The conversion keeps the behaviour of the original robot, including time filtering and the ability to restrict trading direction or to reverse the signal logic.

## Indicators

- **Fast RSI** – Relative Strength Index with configurable period.
- **Slow RSI** – Relative Strength Index with its own period.
- **Moving average on RSI** – Simple moving average calculated on top of each RSI value stream. Both RSIs use the same smoothing length.

All three indicators share the same applied price (close price by default). The two smoothed RSI lines are drawn on a dedicated chart panel for monitoring.

## Entry rules

1. Wait for both smoothed RSI values to form on the current completed bar.
2. **Long setup**
   - The fast smoothed RSI crosses **above** the slow smoothed RSI (current value above, previous value below).
   - Both smoothed RSIs are **below** the neutral level (50 by default).
3. **Short setup**
   - The fast smoothed RSI crosses **below** the slow smoothed RSI (current value below, previous value above).
   - Both smoothed RSIs are **above** the neutral level.
4. Optionally reverse the signal directions using the `ReverseSignals` parameter.
5. Signals generated on the same bar are ignored (one entry per bar).

## Position management

- `AllowLong` and `AllowShort` control whether the strategy may open positions in each direction.
- `CloseOpposite` closes an existing position before entering the opposite side, replicating the original EA logic.
- `OnlyOnePosition` forbids opening a new position when any position is already active.
- Market orders are issued with the strategy `Volume`.

## Time filter

Enable or disable the trading session filter with `UseTimeFilter`. When enabled, trades are allowed only between `SessionStart` and `SessionEnd`. Sessions that cross midnight are supported. The timestamps are evaluated in the exchange time zone provided by the incoming candle messages.

## Parameters

| Name | Description |
| --- | --- |
| `CandleType` | Candle series analysed by the strategy. |
| `FastRsiPeriod` | Period of the fast RSI. |
| `SlowRsiPeriod` | Period of the slow RSI. |
| `MaPeriod` | Moving-average length used to smooth both RSI streams. |
| `AppliedPrice` | Price type forwarded into the RSI calculations. |
| `NeutralLevel` | RSI threshold that separates bullish and bearish zones. |
| `AllowLong` / `AllowShort` | Enable or disable trading direction. |
| `ReverseSignals` | Swap long and short signals. |
| `CloseOpposite` | Close the opposite position before entering a new one. |
| `OnlyOnePosition` | Permit at most one open position. |
| `UseTimeFilter` | Activate the trading session filter. |
| `SessionStart` / `SessionEnd` | Trading window boundaries. |

## Differences from the original EA

- Money management, stop-loss and trailing-stop blocks of the original MQL5 code are not reproduced. The StockSharp strategy places market orders using the fixed `Volume` configured on the strategy.
- All logging and diagnostic alerts were removed; StockSharp logging should be used instead if required.
- Platform-specific transaction tracking is replaced with StockSharp order-state events.

Despite these differences, the core entry logic and directional filters match the source expert advisor.
