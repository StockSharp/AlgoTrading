# KA-Gold Bot Strategy

The **KA-Gold Bot Strategy** is a direct port of the MetaTrader expert advisor "KA-Gold Bot". It trades breakouts of a custom Keltner-style channel and aligns signals with medium-term trend filters. The port relies on StockSharp high-level candle subscriptions, indicator bindings, and strategy parameters so that the behaviour remains configurable from the UI and ready for optimization.

## Trading Logic

- Compute three exponential moving averages (EMA):
  - EMA(10) for fast momentum confirmation.
  - EMA(200) to detect the higher-timeframe trend.
  - EMA(period) as the centre of the channel; the same length is used to average the candle range (high-low).
- Average the daily range with a simple moving average to form dynamic envelopes:
  - Upper band = EMA(period) + SMA(high-low, period).
  - Lower band = EMA(period) − SMA(high-low, period).
- A **long** setup requires all of the following on the last closed candle:
  - Close price above the upper band.
  - Close price above EMA(200).
  - EMA(10) crossed from below the previous upper band to above the latest upper band.
- A **short** setup mirrors the rules:
  - Close price below the lower band.
  - Close price below EMA(200).
  - EMA(10) crossed from above the previous lower band to below the latest lower band.
- Only one position may be open at a time; opposing signals are ignored until the strategy is flat.

## Position Sizing

Two volume models are supported:

1. **Fixed lot mode** – use the `BaseVolume` parameter directly.
2. **Risk percentage mode** – when `UseRiskPercent = true`, the free-equity proxy (`Portfolio.CurrentValue` or `Portfolio.BeginValue`) is multiplied by `RiskPercent`. The result is scaled by 100,000 (MetaTrader lot convention) and rounded to multiples of `BaseVolume`, respecting `Security.MinVolume`, `Security.MaxVolume`, and `Security.VolumeStep`.

## Risk Management

- Stop-loss and take-profit offsets are defined in pips. Pips are converted to absolute price distances using the security step. Three- and five-decimal forex symbols reuse the MetaTrader rule `pip = step × 10`.
- Initial protective orders are registered immediately after the first fill and kept in sync with the current position size.
- Trailing stops activate once unrealised profit reaches `TrailingTriggerPips`:
  - Long positions trail by keeping the stop `TrailingStopPips` away from the close.
  - Short positions use the symmetric distance above the market.
  - The stop is moved only if the distance improves by at least `TrailingStepPips` to avoid over-triggering.
- When the position is closed, pending protective orders are cancelled automatically.

## Session and Spread Filters

- Optional trading window controlled by `UseTimeFilter`, `StartHour`, `StartMinute`, `EndHour`, and `EndMinute` (inclusive-exclusive window). Overnight windows are supported (end earlier than start wraps past midnight).
- An optional spread filter rejects new entries if the current spread (difference between best ask and bid in price steps) exceeds `MaxSpreadPoints`.

## Implementation Notes

- Candles are processed via `SubscribeCandles().Bind(...)`; the EMA(10) and EMA(200) values arrive through the binding, while the channel EMA and range average are updated inside the handler without using `GetValue`.
- Indicator state is stored only through scalar fields mirroring the MetaTrader `iClose` and `CopyBuffer` shift logic, preserving the requirement to compare the last two closed bars.
- Protective and trailing logic uses high-level order helpers (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) to mirror MetaTrader's `PositionModify` calls.
- Portfolio-based sizing depends on available equity information in StockSharp; if it is missing, the strategy falls back to the fixed volume.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `KeltnerPeriod` | Period for the channel EMA and range smoothing. | 50 |
| `FastEmaPeriod` | Length of the fast EMA filter. | 10 |
| `SlowEmaPeriod` | Length of the slow EMA trend filter. | 200 |
| `BaseVolume` | Minimum order volume (lot size). | 0.01 |
| `UseRiskPercent` | Enable balance-based position sizing. | true |
| `RiskPercent` | Percent of equity used per trade when risk sizing is active. | 1 |
| `StopLossPips` | Stop-loss distance in pips. | 500 |
| `TakeProfitPips` | Take-profit distance in pips (0 disables). | 500 |
| `TrailingTriggerPips` | Profit threshold to arm the trailing stop. | 300 |
| `TrailingStopPips` | Distance maintained by the trailing stop once armed. | 300 |
| `TrailingStepPips` | Minimal improvement before the stop is moved. | 100 |
| `UseTimeFilter` | Toggle the trading session filter. | true |
| `StartHour`, `StartMinute` | Session start time. | 02:30 |
| `EndHour`, `EndMinute` | Session end time (exclusive). | 21:00 |
| `MaxSpreadPoints` | Maximum spread allowed in price steps (0 = disabled). | 65 |
| `CandleType` | Timeframe used for signal candles. | 5-minute candles |

## Differences Compared to MetaTrader Version

- The trailing-stop implementation recreates the `PositionModify` sequence using StockSharp stop orders; functionality is equivalent but relies on exchange-confirmed orders.
- MetaTrader calculated channel width from the average high-low range; the port reproduces the same averaging with a simple moving average to keep breakouts identical.
- Risk sizing accesses portfolio equity instead of free margin. This approximation matches the intent (percentage of capital) but may differ if leverage-specific margin data is unavailable.
- Spread checks use `Security.BestAskPrice` and `Security.BestBidPrice`. When depth is not available, the filter is skipped, mirroring the "floating spread" option in the original expert.

## Usage Tips

- Attach the strategy to instruments where pip definition follows forex conventions (3 or 5 decimals) to keep risk parameters aligned with the original expert.
- Optimise the EMA periods and channel length for non-gold instruments because the source strategy was tuned for XAUUSD.
- Monitor the portfolio window to ensure equity values are populated when `UseRiskPercent` is enabled.
