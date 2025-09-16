# True Sort Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the classic "True Sort" template from MetaTrader by waiting for five exponential moving averages to align in strict order. When both the current and previous completed candles respect the same bullish or bearish sorting and the Average Directional Index (ADX) confirms momentum, the strategy opens a position in the direction of the trend. Risk is controlled through optional absolute stop-loss and take-profit distances together with a trailing stop that only activates after price moves far enough in favour of the trade.

## How It Works

1. Build five EMAs (fast to slow: default 10, 20, 50, 100, 200 periods) on the selected candle series.
2. Calculate ADX with a configurable period (default 24) to qualify whether the trend has enough strength (default threshold 20).
3. Only the moment a candle closes do we analyse the indicators. Signals are ignored for unfinished candles to prevent premature decisions.
4. A long setup requires the following for the **current** and **previous** completed candle:
   - `EMA_fast > EMA_2 > EMA_3 > EMA_4 > EMA_slow` (perfect bullish stack).
   - `ADX > threshold` to make sure the slope is meaningful.
5. A short setup mirrors the above with all inequalities reversed.
6. Positions are closed when the ordered stack breaks, when protective levels are hit, or when the trailing stop gives back a configurable amount of profit.

This logic keeps the strategy strictly in strongly trending markets and forces alignment across two bars to reduce noise.

## Trading Rules

- **Entry**
  - **Long**: ADX greater than the threshold and five EMAs sorted from fastest to slowest for both the current and the prior finished candle. Any open short is closed first, then a new long is opened with the configured `Volume`.
  - **Short**: ADX greater than the threshold and EMAs sorted in descending order for two consecutive candles. Any open long is flattened before the short entry is submitted.
- **Exit**
  - If the EMA stack loses its strict ordering the position is immediately closed.
  - Optional protective exits:
    - Stop-loss distance in absolute price units below (long) or above (short) the entry price.
    - Take-profit distance in absolute price units beyond the entry price.
    - Trailing stop that triggers only after price advances by `TrailingStopDistance + TrailingStepDistance` and then follows price at `TrailingStopDistance`.
  - Manual closes or external fills will also reset the internal state.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Data type of candles used for all calculations. | 1-hour time frame |
| `FastEmaLength` | Period of the fastest EMA (entry alignment). | 10 |
| `SecondEmaLength` | Period of the second EMA. | 20 |
| `ThirdEmaLength` | Period of the third EMA. | 50 |
| `FourthEmaLength` | Period of the fourth EMA. | 100 |
| `SlowEmaLength` | Period of the slowest EMA that represents the long-term trend. | 200 |
| `AdxPeriod` | Averaging length for the ADX indicator. | 24 |
| `AdxThreshold` | Minimum ADX value required to allow trades. | 20 |
| `StopLossDistance` | Absolute price distance of the protective stop (0 disables). | 0.005 |
| `TakeProfitDistance` | Absolute price distance of the profit target (0 disables). | 0.015 |
| `TrailingStopDistance` | Distance between the highest/lowest price and the trailing exit. | 0.0005 |
| `TrailingStepDistance` | Extra advance needed before the trailing stop activates or moves. | 0.0001 |

All distance values are expressed in price units. For FX symbols quoted with four or five decimals, values such as `0.005` roughly correspond to 50 pips. Adjust the numbers to match the tick size of the traded instrument.

## Notes & Tips

- Works best on trending instruments such as major FX pairs or indices on intraday or swing time frames. Increase the EMA lengths for daily bars or shorten them for scalping.
- The two-candle confirmation drastically reduces whipsaws but may cause late entries. Consider optimising the ADX threshold and EMA lengths for your market.
- Trailing stops remain idle until price moves by `TrailingStopDistance + TrailingStepDistance` from the entry. Setting the step to zero mimics the MetaTrader behaviour where trailing begins once price covers the base distance.
- The strategy relies on market orders (`BuyMarket`, `SellMarket`). Configure the `Volume` property of the strategy instance to control position sizing or integrate with portfolio money management if required.
- Combine with session filters or higher-timeframe confirmation if you need to limit trading hours.

