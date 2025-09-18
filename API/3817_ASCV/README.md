# ASCV Pivot Breakout Strategy

## Overview

The ASCV Pivot Breakout strategy is a high level StockSharp port of the MetaTrader 4 expert advisor "ASCV" (file `Avpb.mq4`). The original robot combines two custom indicators (ASCTrend1sig and BrainTrend1Sig), a standard deviation filter, daily pivot levels and intraday volume acceleration to trade breakout continuation setups inside a restricted trading window. Because the proprietary custom indicators are not available in StockSharp, the conversion recreates their behaviour through a blend of moving averages, stochastic momentum and daily pivot analytics while preserving the management rules of the EA.

## Trading Logic

1. **Session filter** – trades are allowed only between the configured start and end hours (default 02:00–20:00 broker time). Hourly resets reproduce the MQL logic that clears entry flags whenever `Minute()==0`.
2. **Volatility gate** – a standard deviation indicator built on the selected timeframe must be above a configurable threshold. This mirrors the original `iStdDev` call that required an active market before entries were considered.
3. **Trend confirmation** – a fast and a slow simple moving average estimate the directional filter that ASCTrend/BrainTrend provided. A long signal requires the fast average to be above the slow one and the candle to close above the daily pivot. Shorts expect the opposite configuration.
4. **Momentum confirmation** – a stochastic oscillator ensures that bullish breakouts occur with positive `%K-%D` momentum and that bearish opportunities have negative momentum. The absolute spread between `%K` and `%D` is reused as an adaptive exit trigger just like the EA relied on the difference of the stochastic main/signal lines.
5. **Volume acceleration** – the candle volume must exceed the previous candle volume by the configured delta (default 30 contracts) to approximate the `Volume[0]-Volume[1]` filter.
6. **Order placement** – the strategy uses market orders (`BuyMarket`/`SellMarket`) with fixed volume. Only one trade per direction is allowed per hour in line with the expert advisor.
7. **Stops and targets** – stops are placed at the nearest pivot support/resistance (S1/S2 or R1/R2). If those levels are too close, fallback distances expressed in price steps are applied. Profit targets follow the same hierarchy: R2/R1/Pivot for longs and S2/S1/Pivot for shorts. A fallback distance emulates the EA behaviour when pivots were unavailable.
8. **Dynamic management** – the stochastic spread drives early exits on loss of momentum. A trailing stop measured in price steps mirrors the progressive stop loss modifications from the MQL version.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Timeframe for indicator calculations and signal processing. | 15 minute candles |
| `StartHour` / `EndHour` | Inclusive hour boundaries of the trading session. | 2 / 20 |
| `FastMaLength` | Period of the fast SMA trend filter. | 10 |
| `SlowMaLength` | Period of the slow SMA trend filter. | 40 |
| `StdDevLength` | Lookback length of the standard deviation volatility filter. | 10 |
| `StdDevThreshold` | Minimum standard deviation required to trade. | 0.0005 |
| `VolumeDeltaThreshold` | Minimum difference between current and previous candle volume. | 30 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Periods of the stochastic oscillator. | 5 / 3 / 3 |
| `StochasticExitDelta` | Absolute `%K-%D` spread that triggers momentum exits. | 5 |
| `TrailingStopSteps` | Distance of the trailing stop in price steps. | 30 |
| `MinPivotDistanceSteps` | Minimal distance (in steps) required for pivot based targets. | 50 |
| `StopFallbackSteps` | Stop distance when no pivot support/resistance is far enough. | 33 |
| `TakeProfitBufferSteps` | Fallback take profit distance in price steps. | 50 |
| `OrderVolume` | Volume for every market order. | 1 |

All distances are defined in instrument price steps to ensure compatibility with the exchange specifications.

## Implementation Notes

- The strategy uses the high level `SubscribeCandles().BindEx(...)` pattern. Indicators are **not** added to `Strategy.Indicators`, matching StockSharp guidance.
- Pivot levels are recalculated once per trading day using the previous day's high, low and close. The first day only collects data and starts trading once the second day begins.
- `StartProtection()` is enabled to automatically protect against unexpected disconnections, replicating the EA's safety net.
- XML and inline comments inside the C# code explain the mapping of each block to the original MQL logic.
- Stop loss and take profit values are set via `SetStopLoss`/`SetTakeProfit` using price step conversions to remain broker agnostic.

## Usage Tips

1. Run the strategy on an instrument that exposes both candle data and volume because the volume acceleration filter is essential.
2. When optimising, focus first on the volatility (`StdDevThreshold`) and volume (`VolumeDeltaThreshold`) filters—the original EA was very sensitive to quiet markets.
3. Adjust pivot distances to match the volatility profile of the traded symbol. For high tick size instruments increase `MinPivotDistanceSteps` to avoid premature exits.
4. If the stochastic spread produces too many exits, widen `StochasticExitDelta` so that the trailing stop becomes the dominant exit condition.

## Files

- `CS/AscvStrategy.cs` – the C# implementation of the strategy.
- `README.md` – this documentation.
- `README_ru.md` – Russian translation.
- `README_cn.md` – Chinese translation.
