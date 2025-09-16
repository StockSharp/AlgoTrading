# Slime Mold RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A direct conversion of the "Slime_Mold_RSI_v1.1" MQL4 expert advisor. The strategy builds a single perceptron by combining four RSI readings (12, 36, 108 and 324) calculated on the median price. Each RSI value is normalized from the original 0–100 range into -1…+1 and multiplied by a configurable weight. A zero crossing of the weighted sum flips the position.

## How It Works
- Calculate the median price of each finished candle and feed it into four Relative Strength Index indicators with lengths of 12, 36, 108 and 324.
- Normalize every RSI value to the -1…+1 interval and apply the corresponding weight. The defaults (-100) reproduce the original perceptron coefficients (`x - 100`).
- Sum the four weighted inputs to produce the perceptron output for the current candle.
- Compare the latest value with the previous candle's perceptron output to detect zero crossings and generate trading signals.

## Trading Rules
- **Long entry**: Previous perceptron value is below zero and the current value rises above zero. The strategy closes any short exposure and establishes a long position of size `Volume`.
- **Short entry**: Previous perceptron value is above zero and the current value falls below zero. The strategy exits any long position and opens a short position of size `Volume`.
- **Position management**: There are no explicit profit targets or stop-loss orders. Positions are only changed when a new zero crossing occurs.

## Parameters
- `Weight1` – coefficient applied to the normalized 12-period RSI input.
- `Weight2` – coefficient applied to the normalized 36-period RSI input.
- `Weight3` – coefficient applied to the normalized 108-period RSI input.
- `Weight4` – coefficient applied to the normalized 324-period RSI input.
- `CandleType` – timeframe of the candles supplied to the strategy. The default is 1-hour candles.

## Details
- **Entry Criteria**: Zero crossing of the weighted RSI perceptron.
- **Long/Short**: Both (always in the market after the first signal).
- **Exit Criteria**: Opposite zero crossing reverses the position.
- **Stops**: None.
- **Default Values**:
  - `Weight1` = -100
  - `Weight2` = -100
  - `Weight3` = -100
  - `Weight4` = -100
  - `CandleType` = 1-hour candles
- **Filters**:
  - Category: Perceptron / Oscillator
  - Direction: Bi-directional
  - Indicators: RSI (median price)
  - Stops: No
  - Complexity: Intermediate (requires four long-horizon indicators)
  - Timeframe: Configurable (default intraday hourly)
  - Seasonality: No
  - Neural networks: Linear perceptron
  - Divergence: No
  - Risk level: Depends on chosen volume and weights

## Notes
- The implementation keeps track of the previous perceptron output even when trading is disabled to ensure state continuity once trading resumes.
- Median price is used to match the `PRICE_MEDIAN` setting from the original MetaTrader script.
- The strategy reverses positions instantly, so account for potential slippage when choosing weights and volume.
