# MACD Stochastic 2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the MetaTrader "MACD Stochastic 2" expert logic with StockSharp's high-level API. It combines a three-bar MACD swing filter with a stochastic oscillator to look for momentum reversals near oversold and overbought regions. Risk is controlled through direction-specific stops, take-profits, and an optional trailing stop that operates in pip units.

## Overview

- Works on any instrument and time frame provided through the `CandleType` parameter.
- Uses the MACD main line to confirm local troughs/peaks while the MACD histogram and signal line remain available for visualization.
- Confirms entries with a stochastic %K reading below 20 for longs and above 80 for shorts.
- Adapts MetaTrader pip handling by deriving pip size from the instrument's price step, multiplying by 10 when the symbol has 3 or 5 decimal places.

## Trading Logic

### Long Entry

1. MACD main line values of the current and previous two finished candles are all below zero.
2. The current MACD value is greater than the previous value, while the previous value is less than the value two bars ago (local trough).
3. Stochastic %K is below 20 (oversold).
4. No existing long position is open (`Position <= 0`). Any short position is flattened before entering the new long.

### Short Entry

1. MACD main line values of the current and previous two finished candles are all above zero.
2. The current MACD value is less than the previous value, while the previous value is greater than the value two bars ago (local peak).
3. Stochastic %K is above 80 (overbought).
4. No existing short position is open (`Position >= 0`). Any long position is closed before entering the new short.

### Risk Management & Exits

- **Hard Stop / Take Profit:** Each direction has independent pip-based stop-loss and take-profit distances. Pips are converted to absolute price offsets using the computed pip size.
- **Trailing Stop:** When enabled, the trailing stop activates after price advances beyond the trailing distance. The stop is raised/lowered only when the move exceeds the configured trailing step to avoid excessive order churn.
- **Opposite Signals:** Entering an opposite signal first flat-tens the existing position, then opens the new one with the configured trade volume.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeVolume` | `1` | Order volume sent with each new trade. |
| `StopLossBuyPips` | `50` | Pip distance for long stop-loss. Set to `0` to disable. |
| `StopLossSellPips` | `50` | Pip distance for short stop-loss. Set to `0` to disable. |
| `TakeProfitBuyPips` | `50` | Pip distance for long take-profit. Set to `0` to disable. |
| `TakeProfitSellPips` | `50` | Pip distance for short take-profit. Set to `0` to disable. |
| `TrailingStopPips` | `0` | Trailing stop distance in pips. `0` disables trailing. |
| `TrailingStepPips` | `5` | Minimum pip gain before updating the trailing stop. Must stay positive when trailing is enabled. |
| `MacdFastPeriod` | `12` | Fast EMA length for MACD. |
| `MacdSlowPeriod` | `26` | Slow EMA length for MACD. |
| `MacdSignalPeriod` | `9` | Signal smoothing length for MACD. |
| `StochasticKPeriod` | `5` | Lookback period for stochastic %K. |
| `StochasticDPeriod` | `3` | Smoothing period for stochastic %D. |
| `StochasticSlowing` | `3` | Additional smoothing applied to stochastic %K. |
| `CandleType` | `1h time frame` | Candle type (time frame) used for indicator calculations. |

## Notes

- The pip size calculation mirrors the original MetaTrader expert: `pip = PriceStep` and is multiplied by 10 when the instrument is quoted with 3 or 5 decimals.
- Stochastic thresholds (20/80) remain constants as in the original script. Adjust them directly in code if custom levels are needed.
- The strategy operates on fully finished candles only, ensuring consistency with MetaTrader's bar-close execution.

## Usage

1. Configure the desired instrument, `CandleType`, and volume before starting the strategy.
2. Tune stop, take-profit, and trailing parameters to match the instrument's volatility.
3. Optionally optimize MACD and stochastic lengths using StockSharp's optimizer thanks to the exposed parameters.
4. Monitor the chart objects (candles, MACD, stochastic, own trades) added automatically when a chart area is available.
