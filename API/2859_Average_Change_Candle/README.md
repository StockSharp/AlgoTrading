# Average Change Candle Strategy

Strategy converted from the MetaTrader expert `Exp_AverageChangeCandle`. It recreates the original logic inside StockSharp by smoothing candle ratios relative to a dynamic baseline moving average and reacting to bullish/bearish color transitions.

## Core idea

1. Compute a baseline moving average (`MaMethod1`, `Length1`) over the selected applied price.
2. Express the current candle open and close as ratios to the baseline and raise them to the power `Power`.
3. Smooth the transformed open and close values with a secondary moving average (`MaMethod2`, `Length2`).
4. Classify the candle color: bullish when smoothed close &gt; smoothed open, bearish when smoothed close &lt; smoothed open.
5. Generate trading signals when the color changes after the configured `SignalBar` delay.

Only finished candles are processed. The strategy opens market positions in the direction of the new color and optionally closes the opposite side.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | `1` | Volume used when opening a new position. |
| `MaMethod1` | `Lwma` | Smoothing applied to the baseline ratio (subset of SMA/EMA/SMMA/LWMA/JJMA/AMA). Unsupported types fall back to EMA. |
| `Length1` | `12` | Period of the baseline moving average. |
| `Phase1` | `15` | Jurik phase parameter for the baseline (kept for compatibility). |
| `PriceSource` | `Median` | Applied price used before calculating the baseline. |
| `MaMethod2` | `Jjma` | Smoothing applied to the transformed ratios. |
| `Length2` | `5` | Period of the signal moving average. |
| `Phase2` | `100` | Jurik phase parameter for the signal smoothing. |
| `Power` | `5` | Exponent used when raising the open/close ratios. |
| `SignalBar` | `1` | How many closed bars to wait before acting on a color change. |
| `BuyOpenEnabled` | `true` | Allow opening long positions. |
| `SellOpenEnabled` | `true` | Allow opening short positions. |
| `BuyCloseEnabled` | `true` | Close longs when a bearish signal appears. |
| `SellCloseEnabled` | `true` | Close shorts when a bullish signal appears. |
| `StopLossPoints` | `0` | Absolute stop-loss distance. `0` disables the stop. |
| `TakeProfitPoints` | `0` | Absolute take-profit distance. `0` disables the target. |
| `CandleType` | `H4` time-frame | Candle series processed by the strategy. |

## Trading rules

- **Bullish transition** (`color` changes to 2): close active shorts (if allowed) and open a long position when `Position <= 0` and `BuyOpenEnabled` is true.
- **Bearish transition** (`color` changes to 0): close active longs (if allowed) and open a short position when `Position >= 0` and `SellOpenEnabled` is true.
- Color 1 (neutral) does not trigger trades.
- Signals are evaluated using the bar located `SignalBar` steps behind the most recent finished candle to mimic the original MetaTrader timing.

## Risk management

`StopLossPoints` and `TakeProfitPoints` configure `StartProtection` with absolute distances. When either value is zero the respective protection is disabled.

## Notes

- Only smoothing methods available in StockSharp are implemented directly. JurX, ParMA, T3 and VIDYA from the original code are mapped to EMA as a functional fallback.
- Phase parameters are kept for compatibility but only affect Jurik/Kaufman based averages.
- The strategy uses market orders just like the original expert advisor. Slippage management from the MQL version is not reproduced because StockSharp handles execution via connectors.
