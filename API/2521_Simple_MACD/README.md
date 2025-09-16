# Simple MACD
[Русский](README_ru.md) | [中文](README_cn.md)

Simple MACD replicates the logic of the MQL5 advisor `Simple_MACD.mq5` in StockSharp. The strategy follows the slope of the MACD main line calculated on completed candles and keeps adding to the position whenever the slope remains in the same direction.

## Overview

- **Market**: any instrument with candle data and continuous trading hours.
- **Core indicator**: Moving Average Convergence Divergence (MACD) using exponential moving averages 12/26 and signal 9.
- **Approach**: momentum-following. The strategy compares two most recent completed MACD readings and goes long when the line rises, or short when the line falls.
- **Order type**: market orders only. Every signal aggregates the amount required to close the opposite position and adds the configured trade volume on top, mirroring the original expert advisor.

## Conversion Notes

- The MQL5 bot triggered once per new bar by comparing `MACD(1)` and `MACD(2)` (previous two completed bars). In StockSharp, the same comparison is executed when a candle finishes, before the next bar starts.
- The MQL version relied on explicit position enumeration and manual volume checks. The StockSharp version aggregates volume automatically with `BuyMarket`/`SellMarket` calls and the strategy `TradeVolume` parameter.
- Hedging checks from the MQL code are not required because StockSharp tracks the net position directly.

## Trading Rules

### Entry and Scaling

1. Compute the MACD main line on each finished candle.
2. Store the last two MACD values and compare them:
   - If `MACD(1) > MACD(2)` the slope is bullish. The strategy buys a volume equal to `TradeVolume + max(0, -Position)` to close shorts and add new longs.
   - If `MACD(1) < MACD(2)` the slope is bearish. The strategy sells `TradeVolume + max(0, Position)` to close longs and add new shorts.
3. If both values are equal no new orders are submitted.

### Position Management

- The strategy keeps stacking orders in the current direction as long as the MACD slope does not change sign, just like the original advisor which submitted a buy or sell on every qualifying bar.
- Opposite signals flatten any open exposure before building the new position.
- No stop-loss or take-profit levels are embedded; risk control relies on external money-management rules or manual supervision.

### Additional Safeguards

- Trading is skipped until the MACD indicator becomes fully formed.
- Only completed candles (`CandleStates.Finished`) are processed, preventing premature actions on partial data.
- Log messages trace every trade and show the two MACD values used to make the decision for easier backtesting analysis.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `FastPeriod` | 12 | Fast EMA length for the MACD calculation. |
| `SlowPeriod` | 26 | Slow EMA length for the MACD calculation. |
| `SignalPeriod` | 9 | Signal EMA period retained for compatibility with the original settings. |
| `TradeVolume` | 0.1 | Volume added on each signal before accounting for position reversal. |
| `CandleType` | 1 minute time frame | Candle type used to feed the indicator. Adjustable to any desired timeframe. |

All parameters are exposed as strategy parameters and marked as optimizable where meaningful.

## Visualization

- The strategy automatically creates a chart area (when available) with the price candles and overlays the MACD indicator output.
- Own trades are drawn on the chart to show how frequently the strategy scales positions in trending conditions.

## Recommended Usage

- Apply on trending instruments where momentum persists for several bars; range-bound markets will cause frequent reversals and whipsaw trades.
- Combine with portfolio-level risk management since the base logic has no intrinsic stop mechanism.
- Consider optimizing the `TradeVolume` and MACD periods for the target instrument and timeframe.

## Files

- `CS/SimpleMacdStrategy.cs` – StockSharp implementation of the strategy logic.
- `README.md`, `README_ru.md`, `README_cn.md` – detailed documentation in three languages.

