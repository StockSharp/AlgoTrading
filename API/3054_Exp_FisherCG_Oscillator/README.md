# Exp Fisher CG Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **Exp_FisherCGOscillator** MetaTrader 5 expert advisor to StockSharp's high-level API. It recreates the Fisher Center of Gravity oscillator and its trigger line, evaluates signals on a configurable historical bar, and reproduces the original stop/take workflow with StockSharp orders and risk helpers.

## How It Works

1. **Indicator pipeline** – each finished candle is passed through the Fisher CG oscillator: median prices feed a center-of-gravity loop, values are normalised over the last `Length` bars, and a Fisher transform produces the oscillator line. The trigger line is simply the oscillator delayed by one bar.
2. **Signal extraction** – the strategy inspects two historical readings defined by `SignalBar`. It opens a long when the older oscillator value (`SignalBar + 1`) is above its trigger while the newer value (`SignalBar`) crosses back above the trigger, signalling a bullish turn. Shorts mirror this logic on the bearish side.
3. **Exit handling** – long exits occur as soon as the older oscillator drops below its trigger, while short exits fire when it rises above the trigger, matching the EA's immediate close flags. Opposite entries close the active position before reversing.
4. **Bar-by-bar processing** – everything runs on completed candles from `CandleType`; no intra-bar trades are generated, ensuring deterministic backtests and matching the EA's "new bar" gate.

## Risk Management & Position Sizing

- **Stops/targets** – `StopLossPoints` and `TakeProfitPoints` are expressed in instrument steps and translated into absolute price distances via `Security.PriceStep`.
- **Volume control** – `SizingMode = FixedVolume` sends the constant `FixedVolume`. `SizingMode = PortfolioShare` converts `DepositShare` of the current portfolio value into contracts using the latest close and `VolumeStep`.
- **Single position** – the strategy always flattens before entering the opposite side, avoiding simultaneous hedged positions.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Timeframe subscribed for candles and indicator calculations. |
| `Length` | Fisher CG oscillator period (also used for the normalisation window). |
| `SignalBar` | Number of closed candles back used to read signals; `1` matches the EA default. |
| `AllowLongEntry` / `AllowShortEntry` | Toggle long/short entries. |
| `AllowLongExit` / `AllowShortExit` | Toggle automatic exits for long/short positions. |
| `StopLossPoints` / `TakeProfitPoints` | Protective stop and target distances in price steps. Set to `0` to disable. |
| `FixedVolume` | Volume used in fixed sizing mode. |
| `DepositShare` | Portfolio fraction allocated per trade in `PortfolioShare` mode. |
| `SizingMode` | Chooses between fixed volume and share-based position sizing. |

## Usage Notes

- Align `CandleType` and `SignalBar` with the timeframe used by the original indicator (H8 and bar shift of 1 by default).
- Allow a short warm-up period so the oscillator has enough history to form; the strategy ignores trades until the indicator is fully initialised.
- Stops and targets operate on the candle close. Adjust point values to match your instrument's tick size.
- When `PortfolioShare` sizing is selected, ensure portfolio valuation is available; otherwise the strategy falls back to the fixed volume.

## Differences vs Original EA

- Orders are sent as market orders without the `Deviation_` slippage parameter; StockSharp handles execution with its own slippage settings.
- Money management is simplified to two sizing modes (`FixedVolume` and `PortfolioShare`). The EA's loss-percentage options are intentionally omitted.
- Pending order timestamps (`UpSignalTime`/`DnSignalTime`) are not used. Signals are executed immediately on the processed candle.
