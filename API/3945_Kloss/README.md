# Kloss MQL/8186 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Kloss MQL/8186 Strategy** is a direct conversion of the MetaTrader 4 expert advisor `Kloss.mq4`. It combines a Commodity Channel Index (CCI), a Stochastic oscillator, and a shifted typical price filter to time single-position reversals. The StockSharp version keeps the original entry thresholds, stop-loss and take-profit distances, and volume logic (fixed lot size or percentage-based sizing) while using the high-level candle subscription API.

## Trading Logic

- **Data**: Completed candles of the configured timeframe (default 5 minutes). Indicators are calculated on the same series.
- **Indicators**:
  - CCI with period 10. The absolute value is compared against `±CciThreshold` (default 120).
  - Stochastic oscillator with `%K=5`, `%D=3`, smoothing `=3`. The main `%K` line is checked against oversold/overbought bands.
  - Typical price ( (High + Low + Close) / 3 ) delayed by five completed candles to replicate the shifted LWMA from the expert advisor.
- **Long Entry**:
  - CCI <= `-CciThreshold`.
  - Stochastic %K < `StochasticOversold` (default 30).
  - Previous candle open > typical price from five candles ago.
  - No existing long position (`Position <= 0`). Any open short is closed and reversed into a long in a single market order.
- **Short Entry**:
  - CCI >= `CciThreshold`.
  - Stochastic %K > `StochasticOverbought` (default 70).
  - Previous candle close < typical price from five candles ago.
  - No existing short position (`Position >= 0`). Any open long is closed and reversed into a short with one market order.
- **Position Management**: StockSharp's `StartProtection` issues stop-loss and take-profit orders automatically using the specified point distances. The strategy otherwise holds a single position at all times (flat, long, or short).

## Position Sizing

- **Fixed Volume**: If `FixedVolume > 0`, the strategy always trades that exact volume (after aligning to the instrument's `VolumeStep` and `MinVolume`).
- **Risk Percent**: When `FixedVolume = 0`, the strategy allocates `RiskPercent` (default 0.2) of the account value divided by the latest close to estimate order size. The volume is clamped by `MaxVolume` (default 5) and rounded to the instrument's step.
- **Safeguards**: The method falls back to the minimum tradable volume if account information is missing or the computed value is non-positive.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `CciPeriod` | Number of candles used to calculate the Commodity Channel Index. | 10 |
| `CciThreshold` | Absolute CCI level that triggers entries. | 120 |
| `StochasticKPeriod` | %K period of the Stochastic oscillator. | 5 |
| `StochasticDPeriod` | %D smoothing period. | 3 |
| `StochasticSmooth` | Additional smoothing applied to %K before the signal. | 3 |
| `StochasticOversold` | %K threshold to confirm long entries. | 30 |
| `StochasticOverbought` | %K threshold to confirm short entries. | 70 |
| `StopLossPoints` | Distance in price points for the protective stop. | 48 |
| `TakeProfitPoints` | Distance in price points for the profit target. | 152 |
| `FixedVolume` | Positive value forces a fixed trade volume. | 0 |
| `RiskPercent` | Portfolio fraction converted to volume when `FixedVolume` is zero. | 0.2 |
| `MaxVolume` | Maximum allowed trade volume. | 5 |
| `CandleType` | Candle type/timeframe for indicator calculations. | 5-minute time frame |

## Execution Notes

- **Single Position**: Only one position is kept open. Reversals close the existing position and open the new one with a single market order.
- **Indicator Synchronisation**: The price shift uses the last five completed candles; at least six candles must be processed before the first trade can appear.
- **Stops/Targets**: `StartProtection` converts point-based distances into absolute price offsets using the instrument's `PriceStep`. If `PriceStep` is unknown, the raw point value is applied.
- **Data Requirements**: Works with any instrument providing OHLC candles; volume alignment honours `MinVolume` and `VolumeStep` when available.
- **Differences vs. MT4**: MetaTrader margin calculations are approximated via account equity (`Portfolio.CurrentValue`). When equity data is not available, the strategy reverts to the minimal tradable volume.

## Usage Tips

1. Adjust `CandleType` to the market session used in MetaTrader (M5 in the original template).
2. Review stop distances relative to tick size; point-to-price conversion happens automatically but the values may need tuning for non-forex instruments.
3. For fixed contract sizes, set `FixedVolume` to the desired lot and `RiskPercent` to zero.
4. Enable optimization for the indicator thresholds when calibrating the strategy on new symbols.

