# Blau SM Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
This strategy is a C# conversion of the original MetaTrader 5 expert `Exp_BlauSMStochastic` built around the Blau SM Stochastic oscillator. The indicator measures the distance between price and the recent trading range, applies multiple smoothing stages and compares the result with a smoothed reference line. The strategy works on completed candles (default 4-hour timeframe) and allows trading in both directions.

## Indicator Logic
1. Compute the highest high and lowest low over `LookbackLength` bars.
2. Build a detrended price series: `sm = price - (HH + LL) / 2` where `price` is the applied price type.
3. Smooth the detrended series sequentially by three moving averages with lengths `FirstSmoothingLength`, `SecondSmoothingLength` and `ThirdSmoothingLength` using the selected `SmoothMethod` (SMA, EMA, SMMA or LWMA).
4. Smooth the half-range `(HH - LL) / 2` with the same triple sequence to normalize volatility.
5. Form the main oscillator line as `100 * smoothed(sm) / smoothed(range)`.
6. Smooth the main line with `SignalLength` to obtain the signal line.

The parameter `Phase` is kept for compatibility with the MQL version but is not used by the simplified smoothing engine.

## Trading Modes
- **Breakdown**: monitors zero crossings of the main line. A cross from positive to non-positive opens a long and closes shorts. A cross from negative to non-negative opens a short and closes longs.
- **Twist**: tracks momentum twists. If the main line forms a local trough (value rises after declining), a long entry is triggered, while a local peak (value falls after rising) triggers a short. Opposite positions are closed accordingly.
- **CloudTwist**: observes crossings between the main line and the signal line. A downward cross of the main line through the signal line opens a long and exits shorts, while an upward cross opens a short and exits longs.

Entry and exit switches (`EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit`) allow disabling specific operations while keeping indicator calculations intact.

## Risk Management
`TakeProfitPoints` and `StopLossPoints` convert to absolute price distances using the instrument price step and are passed to the built-in protective block via `StartProtection`. Set them to zero to disable the corresponding limit.

## Parameters
- `CandleType` *(DataType, default: 4-hour time frame)* – timeframe used for candle subscription and indicator calculations.
- `Mode` *(BlauSmStochasticMode, default: Twist)* – selects the signal generation mode (Breakdown, Twist, CloudTwist).
- `SignalBar` *(int, default: 1)* – number of bars to shift indicator values when evaluating signals, reproducing the original `SignalBar` logic.
- `LookbackLength` *(int, default: 5)* – bars used to compute highest and lowest values.
- `FirstSmoothingLength` *(int, default: 20)* – length of the first smoothing stage.
- `SecondSmoothingLength` *(int, default: 5)* – length of the second smoothing stage.
- `ThirdSmoothingLength` *(int, default: 3)* – length of the third smoothing stage.
- `SignalLength` *(int, default: 3)* – smoothing length of the signal line.
- `SmoothMethod` *(BlauSmSmoothMethod, default: EMA)* – moving average family applied to all smoothing stages (SMA, EMA, SMMA, LWMA).
- `PriceType` *(BlauSmAppliedPrice, default: Close)* – applied price used to feed the oscillator (close, open, high, low, median, typical, weighted, simple, quarter, trend-follow variants, Demark).
- `EnableLongEntry` *(bool, default: true)* – allow opening long positions.
- `EnableShortEntry` *(bool, default: true)* – allow opening short positions.
- `EnableLongExit` *(bool, default: true)* – allow closing long positions.
- `EnableShortExit` *(bool, default: true)* – allow closing short positions.
- `TakeProfitPoints` *(int, default: 2000)* – fixed take-profit distance expressed in instrument points.
- `StopLossPoints` *(int, default: 1000)* – fixed stop-loss distance expressed in instrument points.

## Notes
- The smoothing engine currently supports classic moving averages (SMA, EMA, SMMA, LWMA). Exotic modes from the MQL library (JMA, JurX, etc.) are not available in StockSharp and are therefore not included.
- Phase is preserved as a parameter for completeness; adjust it for documentation purposes only.
- Works on any symbol supported by StockSharp. Adjust candle type, smoothing lengths and stops to match instrument volatility.
