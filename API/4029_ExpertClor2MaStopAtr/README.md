# ExpertClor 2MA Stop ATR Strategy

## Overview

ExpertClor 2MA Stop ATR Strategy is a protective module converted from the MetaTrader 4 expert advisor `ExpertCLOR_2MAwampxStATR_v01`. The original robot only supervises existing positions opened by other systems. The StockSharp port keeps the same philosophy: the strategy never submits new orders on its own, it only tracks the active position on the configured timeframe and issues exit orders when the original conditions are met.

## Core Logic

1. **Moving Average Cross Exit**  
   Two configurable moving averages (fast and slow) are evaluated on finished candles. When the fast average crosses below the slow one the strategy liquidates long positions. When the fast average crosses above the slow average it exits short positions. The comparison uses the last two completed candles to mirror the MQL crossing check.
2. **ATR Based Trailing Stop**  
   The custom `StopATR_auto` indicator from the source code is emulated with the built-in Average True Range. A trailing stop candidate is computed as `Close ± ATR × Target`. The candidate is only promoted to the active stop after `AtrShiftBars` candles, reproducing the delayed update controlled by `CountBarsForShift` in the MQL version. Stops tighten in the favourable direction only.
3. **Breakeven Transfer**  
   After price moves by `BreakevenPoints × PriceStep` in favour of the trade the protective level jumps to the entry price plus one price step for long positions or minus one step for shorts. This mimics the original breakeven behaviour that nudges the stop slightly into profit.
4. **Position Awareness**  
   Internal trailing state is cleared whenever the position direction changes or becomes flat, preventing stale stop values from being reused on the next trade.

## Indicators

- Fast moving average with selectable period, method (SMA, EMA, SMMA, WMA) and applied price (Close, Open, High, Low, Typical, Median, Weighted).
- Slow moving average with the same configuration options.
- Average True Range (ATR) used to build the StopATR_auto style trailing envelope.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `MaCloseEnabled` | Enable or disable the moving average cross exit. | `true` |
| `AtrCloseEnabled` | Enable or disable the ATR trailing exit. | `true` |
| `FastMaPeriod` | Period of the fast moving average. | `5` |
| `FastMaMethod` | Method of the fast moving average (Simple, Exponential, Smoothed, Weighted). | `Exponential` |
| `FastPriceType` | Applied price for the fast moving average. | `Close` |
| `SlowMaPeriod` | Period of the slow moving average. | `7` |
| `SlowMaMethod` | Method of the slow moving average. | `Exponential` |
| `SlowPriceType` | Applied price for the slow moving average. | `Open` |
| `BreakevenPoints` | Distance in price steps required to move the stop to breakeven. | `15` |
| `AtrShiftBars` | Number of finished candles to wait before the ATR stop is tightened. | `7` |
| `AtrPeriod` | ATR averaging period. | `12` |
| `AtrTarget` | Multiplier applied to the ATR value when computing the trailing stop. | `2.0` |
| `CandleType` | Candle series used for all calculations. | `5m timeframe` |

## Usage Notes

- Attach the strategy to a security where entries are managed externally. ExpertClor 2MA Stop ATR will only send market exits.
- The strategy requires finished candles from the selected timeframe. Make sure the candle subscription matches the timeframe configured in `CandleType`.
- When the instrument does not expose `PriceStep` the strategy falls back to `1` to keep the breakeven calculation operational.
- ATR trailing requires the indicator to be formed. Until then only the moving average exit and the breakeven transfer can trigger.
- No Python port is provided yet; only the C# implementation is available.

## Conversion Details

- The original StopATR_auto indicator is replicated with the built-in Average True Range and the `AtrShiftBars` delay mechanism that mirrors `CountBarsForShift`.
- Order closing via `OrderModify` in MQL is replaced with simple `ClosePosition()` calls when the simulated stop level is touched.
- English in-code comments document every logical block for easier maintenance and further extensions.
