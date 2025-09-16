# Volatility HFT EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **Volatility HFT EA** MetaTrader 5 expert advisor into the StockSharp high-level API. It reproduces the original logic that buys when the closing price jumps well above a fast simple moving average and waits for a pullback to that average. Order generation, indicator management, and protective exits all follow the guidelines from `AGENTS.md` while keeping the behaviour of the MQL script.

## How It Works

1. **Indicator setup** – a single simple moving average (default length: 5) is calculated on the working timeframe specified by `CandleType`.
2. **New bar detection** – processing happens only once a candle is finished (`CandleStates.Finished`), mirroring the `IsNewBar` checks in the EA.
3. **Warm-up requirement** – the strategy waits for 60 completed candles before evaluating trades, matching the `Bars < 60` guard used in MQL.
4. **Entry filter** – a long setup appears when the latest close is at least `MaDifferencePips` above the SMA (difference converted to price using the instrument's pip size) and the SMA value is higher than it was two bars ago. The original EA used `val[0] < -0.0015` and `MA_Val1[0] > MA_Val1[2]`; this port checks the same conditions without manually storing arrays.
5. **One position at a time** – only long trades are supported because the sell branch was commented out in the source file. A new signal is ignored while a position is open.

## Risk Management

- **Stop loss** – optional protective stop expressed in pips. The code derives the pip size from `Security.PriceStep`, multiplying by 10 when the instrument has 3 or 5 decimal places, reproducing the `_Digits` scaling from MetaTrader.
- **Take profit** – the exit target is anchored to the SMA value captured at entry, mirroring the `mrequest.tp = MA_Val1[0];` call. The strategy closes the position when the candle's low touches the stored SMA level, emulating a limit order at the average.

## Parameters

| Parameter | Description |
| --- | --- |
| `OrderVolume` | Volume used for every market order. |
| `FastMaLength` | Period of the fast simple moving average (default 5). |
| `StopLossPips` | Stop-loss distance in pips; set to `0` to disable. |
| `MaDifferencePips` | Minimum distance (in pips) between the close and the SMA required for a long entry. |
| `CandleType` | Timeframe used for candle subscription and indicator calculations. |

`MinimumBars` is a fixed internal constant equal to `60`, reflecting the EA's requirement for sufficient history.

## Usage

1. Attach the strategy to a security and select the desired `CandleType` (for example, 1-minute bars for high-frequency behaviour).
2. Adjust `FastMaLength`, `MaDifferencePips`, and `StopLossPips` to suit the instrument's volatility. Pip-based inputs are automatically converted using the detected pip size, so the same defaults work on 4- and 5-digit FX symbols.
3. Configure `OrderVolume` to match your portfolio sizing rules. The strategy only submits market orders and will not pyramid positions.
4. Start the strategy. It will subscribe to the chosen candles, build the SMA, wait for 60 warm-up bars, and then evaluate entries on every completed candle.
5. Monitor trade management: exits are triggered either by the SMA touch or by the stop-loss price derived at entry.

## Notes & Differences from the Original EA

- The MQL version requested the minimum lot size via `SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN)`; here the volume is exposed as a parameter to keep the strategy flexible across brokers and asset classes.
- Sell conditions are omitted because they were commented out in `Volatility_HFT_EA.mq5`. The behaviour therefore matches the published script, which only executed the long branch.
- Take profit handling uses candle lows to detect a touch of the moving average instead of registering a limit order, ensuring the same intent works reliably within the StockSharp workflow.
- Manual array management (`CopyRates`, `CopyBuffer`, `ArraySetAsSeries`) is replaced by StockSharp indicator bindings. This reduces boilerplate while preserving the original thresholds and slope comparisons.
- All computations remain candle-based; the strategy does not look back into indicator buffers with `GetValue`, staying compliant with the repository guidelines.
