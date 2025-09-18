# 800BB Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the MetaTrader 4 "800BB" expert advisor using StockSharp's high-level API. It enters mean-reversion trades when price pierces a very long Bollinger Band and immediately re-enters the channel on the next bar. Risk is controlled via ATR-based stop and take-profit distances combined with dynamic position sizing based on the configured risk percentage.

## Overview

- Works on any instrument and timeframe supplied through the `CandleType` parameter.
- Uses an 800-period Bollinger Band with a two standard deviation envelope to detect extreme excursions.
- Confirms entries on the bar that opens back inside the band right after an outside close.
- Sizes orders by estimating the ATR-derived stop distance in pips and applying the selected `RiskPercent` to the current portfolio value.
- Replicates MetaTrader's pip calculation by multiplying the price step by 10 when the symbol has 3 or 5 decimal places.

## Trading Logic

### Long Setup

1. The previous completed candle opened or closed below the lower Bollinger Band, flagging an oversold excursion.
2. The current candle opens at or above that prior lower band level (price has re-entered the channel).
3. No long position is currently active. Any open short is closed before opening the new long.
4. Position size is calculated using the ATR-based stop distance and the configured risk percentage.
5. A market buy order is submitted at the candle open. The stop-loss is placed `StopLossAtrMultiplier × ATR` below the entry, while the take-profit is `TakeProfitAtrMultiplier × ATR` above the entry.

### Short Setup

1. The previous completed candle opened or closed above the upper Bollinger Band, flagging an overbought excursion.
2. The current candle opens at or below that prior upper band level (price has re-entered the channel).
3. No short position is currently active. Any open long is closed before opening the new short.
4. Position size is determined by the same ATR-and-risk-percent calculation.
5. A market sell order is submitted at the candle open. The stop-loss is placed `StopLossAtrMultiplier × ATR` above the entry, while the take-profit is `TakeProfitAtrMultiplier × ATR` below the entry.

### Exit Management

- **Protective orders:** Stop-loss and take-profit levels are tracked internally and evaluated on each completed candle. If either threshold is breached, the position is closed at market.
- **Opposite signals:** When an opposite setup triggers, the current position is flattened before the new order is placed.
- **Visualization:** The original EA could draw vertical lines for potential trades. Chart annotations are not recreated here; instead, the strategy draws candles, the Bollinger Band, and own trades when a chart area is available.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `RiskPercent` | `2` | Percentage of portfolio value risked per trade. |
| `TakeProfitAtrMultiplier` | `1.5` | ATR multiple used to calculate the take-profit distance. |
| `StopLossAtrMultiplier` | `1` | ATR multiple used to calculate the stop-loss distance. |
| `AtrPeriod` | `14` | Lookback period for the ATR indicator. |
| `BollingerPeriod` | `800` | Period of the Bollinger Band moving average. |
| `BollingerDeviation` | `2` | Standard deviation multiplier for the Bollinger Band. |
| `CandleType` | `1 hour` | Timeframe (or any other candle type) used for signal generation. |

## Notes

- Ensure the portfolio adapter supplies `Portfolio.CurrentValue`; otherwise, the risk-based position sizing returns zero and the strategy will not trade.
- If the symbol does not expose a valid price step or tick value, the pip and money-per-pip calculations fall back to conservative defaults.
- The long Bollinger lookback (800 bars) means the first trade can only occur after enough historical data is received to warm up both the Bollinger and ATR indicators.
