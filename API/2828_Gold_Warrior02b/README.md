# GoldWarrior02b Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Algorithmic strategy converted from the MetaTrader expert advisor *GoldWarrior02b*.
It combines an impulse gauge, Commodity Channel Index (CCI) and a simple ZigZag swing detector
to trade close to the end of every 15 minute block.

The implementation targets StockSharp's high-level API and focuses on net positions.
Multi-level hedging from the original script is not supported because StockSharp works with netted positions.

## Concept

- Use a custom impulse indicator that averages the difference between candle open and close prices.
- Evaluate CCI values to detect overbought/oversold reversals and strong momentum spikes.
- Derive a ZigZag swing direction from recent highs and lows to avoid trading against the dominant move.
- Only evaluate signals during the final seconds (>= 45s) of minutes 14, 29, 44 and 59.
- Apply dynamic risk management with stop-loss, take-profit, trailing-stop and a global profit target.

## Entry Rules

A trade is considered only if no position is currently open and the current candle closes within
the time window described above.

### Long Setup
- ZigZag swing is pointing down (recent low is lower than the previous one).
- Either:
  - CCI rises above its previous reading while the previous CCI was below -50, current CCI below -30,
    impulse turns positive and the previous impulse was negative.
  - Or CCI falls below -200, the previous CCI was still lower, impulse remains below the positive threshold
    and the previous impulse is weaker than the current value.

### Short Setup
- ZigZag swing is pointing up (recent high is higher than the previous one).
- Either:
  - CCI drops below its previous reading while the previous CCI was above 50, current CCI above 30,
    impulse turns negative and the previous impulse was positive.
  - Or CCI breaks above 200, the previous CCI was higher, impulse stays above the negative threshold
    and the previous impulse is stronger than the current value.

If the previous impulse stays between the configured buy and sell thresholds, signals are ignored.

## Exit Rules

- **Stop-loss**: closes the position when price crosses the stop distance from the entry price.
- **Take-profit**: closes after hitting the configured profit distance.
- **Trailing stop**: once price advances by `(TrailingStop + TrailingStep)` points, the trailing level follows price
  at a distance of `TrailingStop` points. Crossing the trailing level exits the trade.
- **Global profit target**: closes the position when the unrealized PnL exceeds the specified amount (in account currency).

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `BaseVolume` | Trade size for entries. | `0.1` |
| `StopLossPoints` | Stop distance in points. | `100` |
| `TakeProfitPoints` | Take-profit distance in points. | `150` |
| `TrailingStopPoints` | Base trailing stop distance. | `5` |
| `TrailingStepPoints` | Additional distance before the trailing stop activates. | `5` |
| `ImpulsePeriod` | Period for both CCI and impulse calculations. | `21` |
| `ZigZagDepth` | Minimum bars between new ZigZag swings. | `12` |
| `ZigZagDeviation` | Minimum price move (in points) to confirm a swing. | `5` |
| `ZigZagBackstep` | Minimum bars before accepting a new swing. | `3` |
| `ProfitTarget` | Unrealized profit threshold to close all positions. | `300` |
| `ImpulseSellThreshold` | Impulse threshold for shorts (typically negative). | `-30` |
| `ImpulseBuyThreshold` | Impulse threshold for longs (typically positive). | `30` |
| `CandleType` | Timeframe used for calculations. | `5 minute time frame` |

## Notes

- The impulse indicator is a moving average of the difference between candle open and close values
  scaled by the instrument's price step.
- Trailing and PnL calculations rely on the instrument's `PriceStep` and `StepPrice` to convert
  point distances into account currency.
- The original expert advisor scales position sizes and deploys hedging tiers.
  This StockSharp port keeps a single net position per instrument, matching StockSharp's execution model.
- To replicate the original behaviour more closely, consider enabling a 15 minute candle subscription
  and ensuring tick data latency allows execution shortly after the closing timestamp.

## Disclaimer

This sample is for educational purposes. Before running on live markets, validate the strategy under
realistic data, latency and commission conditions.
