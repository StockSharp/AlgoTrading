# Color JJRSX Time Plus Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Converted from the MetaTrader5 expert `Exp_ColorJJRSX_Tm_Plus`. The strategy trades trend reversals detected with a Jurik-smoothed RSI oscillator and includes optional time-based exits, mimicking the original money-management toggles.

## Overview

- **Idea**: Track the slope of the Color JJRSX oscillator (approximated via RSI smoothed by a Jurik Moving Average). When the oscillator turns up the system can close shorts and optionally open longs, and vice versa for downturns.
- **Market**: Single instrument defined by the connected `Security`.
- **Timeframe**: Configurable; default is 4-hour candles (matching the original EA input).
- **Direction**: Long and short. Each direction can be disabled independently.
- **Order Type**: Market orders through `BuyMarket()` / `SellMarket()`.

## Indicator Stack

1. **Relative Strength Index (RSI)** — base momentum oscillator using the `RSI Length` parameter (mirrors `JurXPeriod`).
2. **Jurik Moving Average (JMA)** — smooths the RSI output with `Smoothing Length` (mirrors `JMAPeriod`). The JMA phase parameter of the MQL version is not exposed by StockSharp and is therefore omitted.
3. **Signal Shift** — reproduces the `SignalBar` parameter. Signals are generated from the value `Signal Shift` bars back and the two preceding values to detect slope changes.

## Trading Logic

### Long Management
- **Entry**: Enabled by `Enable Long Entries`. Requires that the smoothed oscillator was declining two bars ago (`previous > older` is false), turned upward on the last completed bar (`previous < older`), and continues higher on the current bar (`current > previous`). Position must be flat or short.
- **Exit**: If `Exit Long on Downturn` is enabled and the oscillator slopes down (`previous > older`), any open long is closed.

### Short Management
- **Entry**: Enabled by `Enable Short Entries`. Requires the oscillator to turn down (`previous > older`) and continue falling on the current bar (`current < previous`) while the strategy is flat or long.
- **Exit**: If `Exit Short on Upturn` is enabled and the oscillator slopes up (`previous < older`), any open short is covered.

### Time Filter
- `Enable Time Exit` closes positions once their holding time exceeds `Holding Minutes`. This mirrors the original expert's timer that liquidates positions after `nTime` minutes.

### Risk Controls
- `Stop Loss (pts)` and `Take Profit (pts)` are converted into StockSharp protective levels via `StartProtection` using `UnitTypes.PriceStep`.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Indicator Timeframe` | Candle type for the indicator calculations. | 4-hour candles |
| `RSI Length` | Period for the RSI (analogous to JurX period). | 8 |
| `Smoothing Length` | Length of the Jurik MA smoothing (analogous to JMA period). | 3 |
| `Signal Shift` | Number of completed bars to skip before checking slopes (`SignalBar`). | 1 |
| `Enable Long Entries` / `Enable Short Entries` | Allow opening trades in each direction. | true |
| `Exit Long on Downturn` / `Exit Short on Upturn` | Allow oscillator-driven exits for existing positions. | true |
| `Enable Time Exit` | Activate the holding-time based liquidation. | true |
| `Holding Minutes` | Maximum minutes to keep a position open. | 240 |
| `Stop Loss (pts)` | Distance of the protective stop in price steps. | 1000 |
| `Take Profit (pts)` | Distance of the profit target in price steps. | 2000 |

## Notes on Conversion

- The JJRSX histogram buffer from the original indicator is emulated with RSI + Jurik smoothing. Only slope information is used, so the numerical scale differences do not affect decisions.
- Money-management options (`MM`, `MMMode`, `Deviation`) are not ported. StockSharp order sizing should be handled through the `Strategy.Volume` property or external portfolio settings.
- Global variables used in MQL to rate-limit orders are unnecessary here because the strategy reacts only to finished candles.
- All comments and documentation are in English per repository guidelines.
