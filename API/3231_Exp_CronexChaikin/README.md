# Exp Cronex Chaikin Strategy

This strategy ports the MetaTrader expert advisor **Exp_CronexChaikin.mq5** to the StockSharp high-level API. The original robot rebuilds the Chaikin oscillator from accumulation/distribution values, smooths it twice with Cronex "XMA" filters, and trades crossovers between the fast and slow lines. The StockSharp version reproduces the same logic while exposing each stage as configurable parameters.

## Trading logic

1. Subscribe to the configured candle series (`CandleType`).
2. Recalculate the accumulation/distribution (AD) line for every finished candle using the selected `VolumeSource` (tick or real volume).
3. Apply the Chaikin oscillator by smoothing the AD line with two moving averages (`ChaikinFastPeriod`, `ChaikinSlowPeriod`, `ChaikinMethod`) and taking their difference.
4. Smooth the resulting oscillator twice using the Cronex filters controlled by `SmoothingMethod`, `FastPeriod`, `SlowPeriod`, and `Phase`. These two smoothed values correspond to the "fast" and "signal" lines in the original indicator.
5. Look back `SignalBar` completed candles and compare both Cronex lines on that bar and on the previous one.
6. When the fast line is above the slow line, the strategy optionally closes short positions and, if `BuyOpenEnabled` is true, opens a long position if a fresh upward cross was detected on the lookback bar.
7. When the fast line is below the slow line, the opposite actions are executed for short trades, controlled by `SellOpenEnabled` and `BuyCloseEnabled`.
8. Whenever a new position is opened, stop-loss and take-profit orders (expressed in points) are recalculated with `StopLoss` and `TakeProfit`.

Only a single net position is maintained. If the signal direction changes, the strategy combines the volume required to close the current position with the new trade size to mimic MetaTrader's netting behaviour.

## Indicators and smoothing options

- **Chaikin oscillator**: Built by applying the selected `ChaikinMethod` moving average type to the accumulation/distribution line. Available options include simple, exponential, smoothed, and linear-weighted averages.
- **Cronex smoothers**: The `SmoothingMethod` parameter exposes the Cronex XMA family (SMA, EMA, SMMA, LWMA, Jurik JJMA/JurX, Parabolic MA, T3, VIDYA, AMA). The `Phase` parameter influences Jurik-based filters exactly like in the MQL implementation.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Data type of the candles used to compute the indicator. Default is a four-hour timeframe. |
| `ChaikinMethod` | Moving-average method used inside the Chaikin oscillator. |
| `ChaikinFastPeriod` / `ChaikinSlowPeriod` | Fast and slow periods applied to the accumulation/distribution line. |
| `SmoothingMethod` | Cronex smoothing algorithm applied to the Chaikin oscillator values. |
| `FastPeriod` / `SlowPeriod` | Lengths of the fast and slow Cronex lines. |
| `Phase` | Phase parameter for Jurik-based smoothers (range -100 to +100). |
| `VolumeSource` | Selects tick or real volume when calculating the accumulation/distribution line. |
| `SignalBar` | Number of completed bars back that must contain the crossover signal. |
| `BuyOpenEnabled` / `SellOpenEnabled` | Enable or disable opening of long or short trades. |
| `BuyCloseEnabled` / `SellCloseEnabled` | Allow closing the opposite position when an inverse signal appears. |
| `TakeProfit` / `StopLoss` | Profit target and protective stop distances in instrument points applied after each entry. |
| `Volume` | Standard StockSharp position size (acts as the lot size in the original expert). |

## Differences from the MQL version

- Money-management and slippage routines from `TradeAlgorithms.mqh` are replaced by the built-in `Volume`, `SetStopLoss`, and `SetTakeProfit` helpers.
- The StockSharp implementation recomputes the AD line on finished candles only, ensuring deterministic behaviour for testing and live trading.
- Cronex smoothing options rely on StockSharp indicators: Jurik filters are backed by `JurikMovingAverage` (with phase control), while VIDYA and ParMA use exponential approximations consistent with other Cronex conversions.
