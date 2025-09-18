# Triangle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **Triangle v1** MetaTrader expert advisor to the StockSharp high-level API. The original EA combined weighted moving average filters on a higher timeframe, a momentum divergence check, and a very long-term MACD confirmation before placing breakout-style orders. The StockSharp version keeps the multi-timeframe logic while replacing tick-by-tick money management with candle-based protective orders.

## How it Works

1. **Multi-timeframe filters.** The working timeframe (`CandleType`, default 15 minutes) is used for executing trades. Trend and momentum filters are calculated on a higher timeframe (`TrendCandleType`, default 1 hour) to mirror the MQL calls that referenced `T`.
2. **LWMA trend gate.** Fast and slow weighted moving averages (LWMA equivalent) must be aligned. Long setups require the fast LWMA to stay above the slow LWMA; shorts demand the opposite relation.
3. **Momentum deviation.** A 14-period momentum series on the higher timeframe must deviate from the neutral level (100) by at least `MomentumThreshold` on any of the last three completed candles, reproducing the `MomLevelB/MomLevelS` checks.
4. **MACD confirmation.** A very high timeframe (`MacdCandleType`, default 30-day candles ≈ monthly) MACD must show the main line on the correct side of the signal line before trades are allowed, copying the `MacdMAIN0` vs `MacdSIGNAL0` condition.
5. **Protective exits.** Stop loss and take profit distances are configured in price steps. When either level is reached on a completed bar the strategy closes the position with a market order.

## Parameters

| Parameter | Description |
| --- | --- |
| `FastMaPeriod`, `SlowMaPeriod` | Lengths of the higher-timeframe weighted moving averages. |
| `MomentumPeriod` | Period for the momentum filter on the higher timeframe. |
| `MomentumThreshold` | Minimum absolute deviation from 100 required on any of the last three momentum readings. Set to 0 to disable the filter. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD parameters applied to `MacdCandleType`. |
| `StopLossSteps`, `TakeProfitSteps` | Protective stop and target distances measured in instrument price steps (ticks). Use 0 to disable. |
| `CandleType` | Trading timeframe used for order execution. |
| `TrendCandleType` | Higher timeframe that feeds LWMAs and momentum. |
| `MacdCandleType` | Timeframe used for the MACD confirmation filter. |

## Usage

1. Select a security and configure `CandleType`, `TrendCandleType`, and `MacdCandleType` to match the timeframes you want to analyse.
2. Adjust MA, momentum, and MACD lengths if you want to adapt the system to a different market or volatility regime.
3. Set `StopLossSteps` and `TakeProfitSteps` according to the instrument tick size. The strategy converts the step counts to actual price distances automatically.
4. Start the strategy. It subscribes to all required candle streams, updates indicators with the high-level `Bind` API, and manages the position when stops or targets are hit.

## Differences from the Original EA

- Money-based exits (`Use_TP_In_Money`, `Use_TP_In_percent`) and the balance-protection block are not recreated because StockSharp works in instrument units. Equivalent behaviour can be achieved by tuning `StopLossSteps`/`TakeProfitSteps`.
- Trailing-stop, break-even, and equity-stop logic from the EA relied on tick processing and MetaTrader-specific order modification calls. The port keeps the simpler fixed-stop approach for clarity; users can extend `UpdatePositionState` with trailing rules if desired.
- Manual trendlines (`TREND`/`TRENDLOW`) and fractal arrays were used as discretionary filters in the EA. They are intentionally omitted so the StockSharp strategy remains fully systematic.
- The strategy always holds at most one net position, which matches typical usage even though the EA exposed a `Max_Trades` parameter.

Tune the thresholds and timeframe parameters to fit the instrument you trade. Wider values are usually required for volatile markets to avoid being filtered out by small momentum fluctuations.
