# Stochastic CG Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **Exp_StochasticCGOscillator** MetaTrader 5 expert advisor to StockSharp. The conversion keeps the original logic of the Stochastic Center of Gravity oscillator, rebuilds the trigger line smoothing, and executes trades using StockSharp's high-level strategy API.

## How It Works

1. **Indicator pipeline** – every finished candle from `CandleType` feeds the custom Stochastic CG oscillator. Median prices drive a center-of-gravity loop, values are normalised over the last `Length` bars, and a weighted rolling window produces the oscillator line. The trigger line is recreated through the same `0.96 * (previous + 0.02)` smoothing that the EA applies.
2. **Signal sampling** – the strategy inspects two historical readings separated by `SignalBar`. A buy is prepared when the older reading (shift `SignalBar + 1`) is above the trigger while the newer reading (shift `SignalBar`) crosses back below it. Shorts mirror the logic in the opposite direction.
3. **Position management** – long positions are closed as soon as the older reading drops below the trigger, while short positions exit when the older reading climbs above it. When a fresh entry appears on the opposite side, the current position is flattened before the reversal order is sent.
4. **Risk handling** – optional stop-loss and take-profit distances are expressed in instrument steps and evaluated on the closing price of each processed candle. They mirror the EA's protective inputs without relying on pending orders.
5. **Warm-up control** – the strategy waits until the indicator is fully initialised (enough history for the CG loop and the four-value smoothing buffer) before emitting signals, guaranteeing deterministic backtests.

## Risk Management & Position Sizing

- **Stops/targets** – `StopLossPoints` and `TakeProfitPoints` translate into absolute distances using `Security.PriceStep`. A value of `0` disables the respective limit.
- **Single active position** – the algorithm never keeps both long and short exposure at the same time. Opposite signals trigger an explicit close before entering the new direction.
- **Position sizing** – `SizingMode = FixedVolume` always trades `FixedVolume`. `SizingMode = PortfolioShare` converts `DepositShare` of the portfolio value into contracts using the latest close and `Security.VolumeStep`.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Timeframe subscribed for candles and indicator calculations. |
| `Length` | Period of the Stochastic CG oscillator (affects CG and normalisation windows). |
| `SignalBar` | Number of closed candles back used to evaluate signals (`1` reproduces the EA default). |
| `AllowLongEntry` / `AllowShortEntry` | Toggle long/short entries. |
| `AllowLongExit` / `AllowShortExit` | Toggle automatic exits for long/short positions. |
| `StopLossPoints` / `TakeProfitPoints` | Protective distances in price steps. Set to `0` to disable. |
| `FixedVolume` | Order size used when sizing mode is fixed volume. |
| `DepositShare` | Portfolio fraction used in share-based sizing. |
| `SizingMode` | Chooses between fixed volume and share-based position sizing. |

## Usage Notes

- Align `CandleType` with the timeframe used by the original indicator (H8 in the MQL version). Larger `SignalBar` values require a longer warm-up because the indicator history buffer must cover the shift.
- Stops and targets act on candle closes; they are not intrabar orders. Adjust the point values to suit the instrument's tick size.
- When `PortfolioShare` sizing is enabled, ensure that portfolio valuation is available; otherwise the strategy falls back to the fixed volume.
- The indicator outputs values in the `[-1, 1]` range like the original implementation, allowing you to reuse familiar threshold-based filters if desired.

## Differences vs Original EA

- Market orders are sent immediately without the `Deviation_` parameter; slippage handling is delegated to the StockSharp execution layer.
- Money management is simplified to two modes (`FixedVolume` and `PortfolioShare`). The EA's additional margin-based sizing options are not reproduced.
- Pending order timestamps (`UpSignalTime` / `DnSignalTime`) are unnecessary because StockSharp strategies work on completed candles and execute synchronously.
