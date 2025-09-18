# Wedge Pattern Strategy

## Overview

The **Wedge Pattern Strategy** is a conversion of the MetaTrader expert advisor *Wedge pattern.mq4* into the StockSharp high-level API. The strategy searches for symmetrical wedge consolidations derived from Bill Williams fractals and trades the breakouts when trend and momentum filters align.

The high-level implementation replaces the original manual order management with StockSharp features while preserving the decision logic:

- **Trend filter** – compares a fast and a slow Linear Weighted Moving Average (LWMA) calculated on typical prices.
- **Momentum filter** – evaluates the absolute distance of the 14-period momentum indicator from its neutral level (100). The last three momentum readings must exceed a configurable threshold.
- **MACD confirmation** – requires the MACD main line to be above the signal line for longs (or below for shorts).
- **Fractal wedge detection** – collects upper and lower fractal points to build converging trendlines. Trading signals are produced when price closes beyond these trendlines plus a configurable confirmation buffer.
- **Risk management** – mimics the MQL implementation with fixed stop-loss and take-profit distances, automatic break-even move, and trailing stop adjustments.

## How it Works

1. Subscribe to a single timeframe defined by the `CandleType` parameter.
2. Update indicator values with each completed candle and maintain rolling buffers for highs and lows to detect new fractals.
3. Build wedge trendlines from the two most recent high and low fractals. Only converging wedges (lower highs and higher lows) are considered valid setups.
4. A long trade is opened when:
   - Fast LWMA > Slow LWMA.
   - MACD line > signal line.
   - Any of the last three momentum readings exceeds the configured threshold.
   - The current candle closes above the projected upper trendline by at least the breakout buffer.
5. A short trade mirrors the conditions with the lines and thresholds inverted.
6. After entry the strategy immediately places stop-loss and take-profit orders. It can later move the stop to break-even and trail it as the position becomes profitable.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Timeframe used for analysis and orders. |
| `FastMaPeriod` | Length of the fast LWMA filter. |
| `SlowMaPeriod` | Length of the slow LWMA filter. |
| `MomentumPeriod` | Lookback period for the momentum indicator (default 14). |
| `MomentumThreshold` | Minimum distance from 100 required from the momentum indicator to consider the market impulsive. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Standard MACD configuration. |
| `FractalDepth` | Number of bars on each side required to confirm a fractal high or low. |
| `StopLossPips` | Initial protective stop distance in pips. |
| `TakeProfitPips` | Initial profit target distance in pips. |
| `UseBreakeven`, `BreakevenTriggerPips`, `BreakevenOffsetPips` | Enable and configure break-even automation. |
| `UseTrailing`, `TrailingActivationPips`, `TrailingDistancePips`, `TrailingStepPips` | Enable and configure trailing-stop behaviour. |
| `BreakoutBufferPips` | Extra buffer applied to the wedge breakout confirmation. |

All pip-based settings are converted to price distances using the security's tick size. The default pip calculation accounts for fractional pricing (3 or 5 decimal places) exactly as in the original Expert Advisor.

## Usage Guidelines

1. Attach the strategy to the desired instrument and select the candle timeframe matching the original setup (e.g., 15-minute candles).
2. Configure position size via the base `Strategy.Volume` property.
3. Optionally adjust filter and risk parameters to match the target market's volatility.
4. Start the strategy; it will subscribe to candles, draw chart data, and trade automatically once wedge breakouts occur.

## Differences from the MQL Version

- The StockSharp version uses high-level `SubscribeCandles` and indicator binding APIs, avoiding manual tick processing.
- Trailing stop and break-even management rely on `SetStopLoss`/`SetTakeProfit`, integrating with the built-in protective behaviour.
- Only one position is maintained at a time; the MetaTrader script supported pyramiding up to a maximum number of trades.
- Alert, mail, and notification functions are omitted; event handling should be implemented externally if needed.

Despite these adaptations, the core entry logic and protective rules follow the original MetaTrader expert closely while using idiomatic StockSharp patterns.
