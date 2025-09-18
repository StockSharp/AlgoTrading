# MasterMind 2 Strategy

## Overview
MasterMind 2 is a conversion of the "TheMasterMind2" MQL4 expert advisor. The strategy waits for extreme values on the Stochastic Oscillator and Williams %R indicators to detect exhaustion points. When both indicators show extreme oversold conditions it opens a long position, and when they both show extreme overbought conditions it opens a short position. The logic operates on fully closed candles only, mimicking the original Expert Advisor behaviour.

## Indicators
- **Stochastic Oscillator** – configured with a long lookback to gauge overbought and oversold levels. The %D signal line is compared against thresholds.
- **Williams %R** – confirms the strength of the extreme by requiring readings close to -100 for longs and near 0 for shorts.

## Entry Rules
1. Wait for a candle to close.
2. Calculate the Stochastic Oscillator and take its %D signal value.
3. Calculate Williams %R over the configured lookback.
4. **Long entry**: if `%D < 3` and `Williams %R < -99.9`, close any existing short exposure and buy.
5. **Short entry**: if `%D > 97` and `Williams %R > -0.1`, close any existing long exposure and sell.

## Exit Rules
- Stop loss and take profit levels are applied relative to the entry price using configurable point distances.
- Trailing stop can tighten the protective stop once the price moves favourably by the specified step.
- A break-even option moves the stop loss to the entry level after the trade accumulates the required profit distance.
- Opposite signals immediately close the current position before opening a new one.

## Parameters
- `Trade Volume` – contract volume submitted with each market order.
- `Stochastic Period`, `Stochastic %K`, `Stochastic %D` – parameters of the Stochastic Oscillator.
- `Williams %R Period` – lookback period for the Williams %R calculation.
- `Stop Loss`, `Take Profit` – protective distances in price points.
- `Trailing Stop`, `Trailing Step` – control dynamic stop management.
- `Break Even` – distance in points required to lock in the entry price.
- `Candle Type` – timeframe or custom candle type used in calculations.

## Notes
- The strategy relies exclusively on finished candles, matching the original MQL4 implementation.
- All orders are issued at market with volume defined by `Trade Volume`.
- Enable or disable the protective features by setting the distance parameters to zero.
