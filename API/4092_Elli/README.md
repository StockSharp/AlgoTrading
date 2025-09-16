# Elli Strategy

## Overview
The Elli Strategy ports the MetaTrader 4 expert advisor "Elli" to the StockSharp high level API. The original robot combined the Ichimoku Kinko Hyo structure on the H1 timeframe with a lower timeframe ADX filter and strict risk parameters. The conversion keeps the same directional logic, replaces manual order management with `StartProtection`, and exposes every tuning knob as an optimisable `StrategyParam<T>` so that the behaviour can be adapted to different markets.

## Trading Logic
1. **Ichimoku trend structure**
   - The strategy subscribes to the timeframe defined by `CandleType` (H1 by default) and computes Tenkan-sen, Kijun-sen and Senkou spans using the original periods (19, 60, 120).
   - A bullish setup requires Tenkan > Kijun > Senkou Span A > Senkou Span B with the candle close above Kijun. Bearish setups mirror this condition.
   - The absolute distance between Tenkan and Kijun must exceed `TenkanKijunGapPips` pips to avoid flat or ranging clouds.
2. **Directional Movement confirmation**
   - A second candle subscription runs the Average Directional Index on the timeframe specified by `AdxCandleType` (M1 by default).
   - Long signals are allowed only when the previous +DI value is below `ConvertLow` and the current +DI pushes above `ConvertHigh`. Shorts require the same relationship for the −DI component, replicating the acceleration filter present in the MT4 code.
3. **Entry execution**
   - When all filters align, the strategy issues a market order with volume `OrderVolume + |Position|`. This automatically closes any opposite exposure before joining the trend.
   - Only one directional exposure is kept at a time, following the original `OrdersTotal() < 1` guard.
4. **Risk management**
   - `StartProtection` attaches symmetric stop loss and take profit orders converted from pip distances using the instrument’s pip size.
   - The position is otherwise managed passively, letting the protection orders handle exits just like the MT4 expert advisor.

## Indicators and Data Subscriptions
- Primary candles: `CandleType` (default 1-hour candles) for Ichimoku processing.
- ADX candles: `AdxCandleType` (default 1-minute candles) for DI acceleration checks.
- Indicators: `Ichimoku` (Tenkan, Kijun, Senkou Span B) and `AverageDirectionalIndex` (providing +DI/−DI).
- Both subscriptions support chart rendering through `DrawCandles`, `DrawIndicator`, and `DrawOwnTrades` if a chart area is available.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `OrderVolume` | `1` | Base market order volume. |
| `TakeProfitPips` | `60` | Take-profit distance expressed in pips. |
| `StopLossPips` | `30` | Stop-loss distance expressed in pips. |
| `TenkanPeriod` | `19` | Tenkan-sen period for the Ichimoku indicator. |
| `KijunPeriod` | `60` | Kijun-sen period for the Ichimoku indicator. |
| `SenkouSpanBPeriod` | `120` | Senkou Span B period for the Ichimoku cloud. |
| `TenkanKijunGapPips` | `20` | Minimum Tenkan/Kijun distance (in pips) required before trading. |
| `ConvertHigh` | `13` | DI threshold the current value must exceed to confirm momentum. |
| `ConvertLow` | `6` | DI threshold the previous value must stay below before a new trade. |
| `AdxPeriod` | `10` | Period used for the ADX computation. |
| `CandleType` | `H1` | Timeframe that drives the Ichimoku calculation. |
| `AdxCandleType` | `M1` | Timeframe used for ADX and DI monitoring. |

All parameters are implemented with `StrategyParam<T>` helpers, enabling optimisation and runtime adjustments inside StockSharp Designer.

## Implementation Notes
- The pip conversion follows the standard forex convention (0.0001 for 5-digit quotes and 0.01 for 3-digit instruments) to preserve the original pip-based thresholds.
- ADX values are cached in `_latestPlusDi`, `_previousPlusDi`, `_latestMinusDi`, and `_previousMinusDi`, ensuring the DI acceleration check matches the MQL `iADX` calls with shifts 0 and 1.
- `IsFormedAndOnlineAndAllowTrading()` blocks signals until the strategy, indicators, and data feeds are ready, preventing premature trades during warm-up.
- Market entries rely on `Volume + Math.Abs(Position)` so that direction changes instantly flatten existing trades, emulating the single-position behaviour of the MT4 script.
