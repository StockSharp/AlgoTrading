# Franks 4 Hour Limit Orders Strategy

## Overview
The **Franks 4 Hour Limit Orders Strategy** ports the MetaTrader 4 expert advisor from `MQL/7684/Franks_4hour_limit_orders.mq4` to the StockSharp high-level API. The original EA combines Alexander Elder's Triple Screen ideas: it evaluates momentum on a four-hour chart using the MACD histogram (OsMA) together with the Force Index, and then places contrarian limit orders around the previous candle extremes. The StockSharp implementation keeps this multi-indicator logic while following the repository guidelines (tabs, high-level API, no custom collections) and adds extensive inline comments in English for clarity.

## Trading Logic
1. **Data Source** – The strategy subscribes to a configurable candle type that defaults to four-hour candles. All calculations are performed only on completed candles to match the MT4 expert's behaviour.
2. **Indicators** – Two managed indicators are used:
   - `MovingAverageConvergenceDivergenceSignal(12, 26, 9)` provides both the MACD line and the signal line. Their difference recreates the OsMA histogram used in the EA.
   - `ForceIndex(24)` measures the force of the previous candle. Only final indicator values are considered.
3. **Historical Context** – The EA requires two completed candles to determine indicator slopes. The port stores the previous OsMA values, the previous Force Index value, and the previous candle high/low to mirror this requirement.
4. **Sell Setup** – When the OsMA histogram increases (`OsMA[1] > OsMA[2]`) and the previous Force Index value is negative, the robot plans a contrarian sell limit order:
   - The base price is the previous candle high plus one point.
   - A safety buffer of 16 pips (configurable) is enforced relative to the current bid. The target price becomes the maximum between the base price and `Bid + buffer`.
   - Stop-loss and take-profit prices are aligned to the instrument price step using the configured pip distances (35 pips and 150 pips by default).
5. **Buy Setup** – When the OsMA histogram decreases (`OsMA[1] < OsMA[2]`) and the previous Force Index is positive, the strategy prepares a buy limit order below the market:
   - The base price is the previous candle low minus one point.
   - The algorithm enforces the same 16-pip buffer relative to the current ask, choosing the minimum between the base price and `Ask - buffer`.
6. **Pending Order Maintenance** – If the OsMA slope flips in the opposite direction before execution, the corresponding pending order is cancelled. When one side fills, the opposite pending order is removed to avoid double exposure.
7. **Position Management** – Upon execution, the fill price is stored and the precomputed stop-loss and take-profit levels are activated. The strategy also implements a pip-based trailing stop (30 pips by default) that moves the protective stop only in the favourable direction when price advances beyond the entry plus the trailing distance.
8. **Exits** – Protective orders are monitored on every completed candle. A long position is closed if the candle low touches the stop or the candle high reaches the target. Short positions use the mirrored rules.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | 1 | Fixed volume used for pending limit orders. |
| `StopLossPips` | 35 | Distance, in pips, between the entry price and the protective stop. |
| `TakeProfitPips` | 150 | Distance, in pips, between the entry price and the take-profit level. |
| `TrailingStopPips` | 30 | Distance, in pips, for the trailing stop that locks in profits once price moves far enough. |
| `EntryBufferPips` | 16 | Minimum gap, in pips, between the current market price and the pending order. |
| `PipSize` | 0.0001 | Pip size used for price conversions; defaults to 0.0001 but can be aligned with exotic symbols. |
| `CandleType` | 4h timeframe | Candle series processed by the strategy. |

## Files
- `CS/Franks4HourLimitOrdersStrategy.cs` – Main C# implementation with detailed English comments.
- `README.md` – This English description of the algorithm.
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.

## Implementation Notes
- The strategy relies solely on the high-level API (`SubscribeCandles`, indicator bindings, and convenience order helpers).
- All price calculations are aligned to the instrument's price step to avoid invalid levels.
- State variables store only the necessary historical data, complying with the repository rule that forbids custom collections.
- Stop-loss, take-profit, and trailing stop management are performed inside the candle processing routine to emulate the MT4 trailing behaviour.
