# AMA Trader v2.1 Strategy

The AMA Trader v2.1 strategy is a conversion of the MetaTrader 4 expert advisor **AMA_TRADER_v2_1.mq4** that combines Kaufman's Adaptive Moving Average (AMA) bursts with a double-smoothed Heiken Ashi filter and RSI momentum checks.

## Core Logic

1. **Adaptive Trend Filter** – A custom AMA engine reproduces the original indicator, including the fast/slow constants, efficiency ratio, and the power parameter. The algorithm watches for momentum bursts where the AMA value jumps by more than `AmaThreshold` price steps compared with the previous bar.
2. **Heiken Ashi Confirmation** – Price candles are smoothed twice: first by a configurable moving average on the raw OHLC prices, then by a second moving average on the Heiken Ashi buffers. A bullish (close above open) smoothed bar allows long trades, while a bearish bar allows shorts.
3. **RSI Momentum Check** – A classic RSI with configurable period confirms momentum: longs require the RSI to pull back from a previous value while staying below 70, shorts require a bounce while the oscillator remains above 30.
4. **Position Management** – The strategy opens a single position at a time, applies optional stop-loss and take-profit distances (in price steps), and can trail the stop once price moves in the trade direction. When RSI crosses the 70/30 extremes, an optional partial close is performed before a full exit occurs on the next crossing.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 15-minute candles | Timeframe for all calculations. |
| `TradeVolume` | 0.1 | Base market order volume. |
| `AmaLength` | 9 | Lookback used by the adaptive moving average. |
| `AmaFastPeriod` | 2 | Fast constant in bars for the AMA smoothing. |
| `AmaSlowPeriod` | 30 | Slow constant in bars for the AMA smoothing. |
| `AmaPower` | 2 | Exponent applied to the smoothing constant (matches `G` in the MQ4 code). |
| `AmaThreshold` | 2 steps | Minimum AMA change (in price steps) to trigger a signal. |
| `FirstMaMethod` | Smoothed | First smoothing method for Heiken Ashi construction. |
| `FirstMaPeriod` | 6 | Length of the first smoothing moving average. |
| `SecondMaMethod` | LinearWeighted | Second smoothing method applied to the Heiken Ashi buffers. |
| `SecondMaPeriod` | 2 | Length of the second smoothing moving average. |
| `RsiPeriod` | 14 | RSI period used by the momentum filter. |
| `PartialClosePercent` | 70% | Portion of the active position to close when RSI crosses an extreme. Set to `0` to disable. |
| `StopLossSteps` | 50 | Stop-loss distance expressed in instrument price steps. Set to `0` to disable. |
| `TakeProfitSteps` | 100 | Take-profit distance expressed in price steps. Set to `0` to disable. |
| `TrailingSteps` | 30 | Trailing stop distance in price steps. Set to `0` to disable trailing. |

## Trading Rules

- **Long Entry** – When the AMA jump is positive and exceeds `AmaThreshold`, the latest smoothed Heiken Ashi candle is bullish, and RSI is pulling back (previous value greater than the current value) while staying at or below 70.
- **Short Entry** – When the AMA jump is negative beyond `AmaThreshold`, the smoothed Heiken Ashi candle is bearish, and RSI is rising (previous value less than current) while staying at or above 30.
- **Partial Close** – If enabled, close `PartialClosePercent` of the position when RSI crosses above 70 (longs) or below 30 (shorts).
- **Full Exit** – Close the entire position on the opposing RSI extreme, on stop-loss, take-profit, or when the trailing stop is hit.

The implementation uses the high-level StockSharp API: a candle subscription feeds the custom AMA calculator, the Heiken Ashi smoothing pipeline, and the RSI indicator. All comments in the source code are in English, mirroring the requirements from the conversion guidelines.
