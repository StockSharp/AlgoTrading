# JMaster RSX Strategy

## Overview
The JMaster RSX strategy is a straight conversion of the MetaTrader 4 expert advisor **jMasterRSXv1**. The system aligns Jurik RSX oscillator values calculated on a fast (M5) and a slow (M30) timeframe. When the higher timeframe points in a bullish or bearish direction and the fast oscillator reaches oversold/overbought territory, the strategy enters a position in the corresponding direction. All signals are evaluated on the open of the new bar using the previous fully closed candles, matching the MT4 implementation that referenced `shift = 1` values.

## Indicators and Data
- **Jurik RSX (Length = `RsxLength`) on the fast timeframe** – evaluates the oscillator on the candle series defined by `FastCandleType` (default 5-minute bars). The conversion reproduces the original recursive filter used by the custom `rsx.mq4` indicator.
- **Jurik RSX on the slow timeframe** – calculated with the same length on the candle series defined by `SlowCandleType` (default 30-minute bars). The latest completed slow value is delayed by one bar before being used, mirroring the MT4 shift behaviour.

## Entry Logic
1. Wait for a new fast candle to open (equivalent to processing a finished candle in StockSharp).
2. Retrieve the previous fast RSX reading and the previous slow RSX reading (one slow candle behind the current close).
3. **Long setup:** slow RSX is above the `MidlineLevel` (default 50) *and* fast RSX is below the `OversoldLevel` (default 25).
4. **Short setup:** slow RSX is below the `MidlineLevel` *and* fast RSX is above the `OverboughtLevel` (default 75).
5. Open a market order with volume `Volume` when no position is currently active.

## Exit Logic
- Close an open long position as soon as the short conditions are met (slow RSX below the midline and fast RSX above the overbought threshold).
- Close an open short position as soon as the long conditions are met (slow RSX above the midline and fast RSX below the oversold threshold).
- The strategy does not stack positions; it always reduces to a flat state before considering a new entry.

## Position Sizing
- Orders are placed with a fixed volume controlled by the `Volume` parameter (default `0.1`).
- No adaptive money-management or pyramiding logic is implemented. This mirrors the default behaviour of the original EA when `DecreaseFactor` was left at zero.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `FastCandleType` | Candle type for the fast RSX calculation | `M5` |
| `SlowCandleType` | Candle type for the slow RSX calculation | `M30` |
| `RsxLength` | Lookback length shared by both RSX instances | `14` |
| `OverboughtLevel` | Fast RSX threshold for short entries | `75` |
| `OversoldLevel` | Fast RSX threshold for long entries | `25` |
| `MidlineLevel` | Slow RSX midline separating bullish/bearish regimes | `50` |
| `Volume` | Order volume for market entries | `0.1` |

## Usage Notes
- Ensure historical data delivers finished candles for both configured timeframes; the strategy only reacts after a candle closes.
- Because the slow RSX value is deliberately delayed by one bar, intrabar reversals on the higher timeframe will appear one bar later—this matches the source EA and prevents look-ahead bias.
- The embedded RSX indicator outputs values in the 0–100 range, allowing direct comparison with other oscillators if desired.
