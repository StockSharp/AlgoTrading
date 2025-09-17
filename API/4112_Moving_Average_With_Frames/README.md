# Moving Average with Frames
[Русский](README_ru.md) | [中文](README_cn.md)

Conversion of the MetaTrader 5 expert advisor **"Moving Average with Frames"**. The original system evaluates the relationship between each candle's open/close prices and a shifted simple moving average (SMA) while displaying multiple optimization "frames" on charts. This StockSharp port focuses on the trading logic: it reacts only once per completed bar, opens a single netting position, and mirrors the money-management rules from the source code.

## Trading Logic

- **Data source** – the strategy subscribes to the configured time frame (`CandleType`) and processes only finished candles, which reproduces the MetaTrader constraint `if(rt[1].tick_volume>1) return;`.
- **Indicator** – a simple moving average with period `MovingPeriod`. The indicator output is shifted forward by `MovingShift` completed candles by keeping a buffer of past values.
- **Warm-up** – trading is suspended until at least 100 completed candles are collected, matching the original `Bars(_Symbol,_Period)>100` guard.
- **Entry conditions**
  - Go **long** when the candle opens below the shifted SMA and closes above it.
  - Go **short** when the candle opens above the shifted SMA and closes below it.
  - The engine enforces a single position: opposing exposure is flattened before entering in the new direction.
- **Exit conditions** – an existing long is closed when the open price is above and the close price is below the shifted SMA; shorts are closed on the opposite crossover. New trades are not opened on the same bar after an exit, just like the original expert.

## Position Sizing and Risk

- **MaximumRisk** – determines the raw order volume as `Portfolio.CurrentValue * MaximumRisk / price` when portfolio data are available. If the broker feed does not provide equity information, the strategy falls back to the manual `Volume` property.
- **DecreaseFactor** – after more than one consecutive losing trade, the next position size is reduced by `volume * losses / DecreaseFactor`, mimicking MetaTrader's lot reduction logic. Any profitable trade resets the counter.
- **Volume alignment** – the computed size is normalized to the instrument's `VolumeStep`, clamped between `MinVolume` and `MaxVolume`, and rounded to two decimals when the exchange does not publish a step.

## Additional Notes

- The MetaTrader "frames" visualization is not ported because StockSharp already provides rich optimization dashboards. The trading logic, signal timing, and sizing behaviour remain faithful to the source.
- All indicator values are consumed directly from the `Bind` callback; no manual `GetValue` calls are used.
- Consecutive loss tracking is implemented inside `OnOwnTradeReceived`, allowing the strategy to react correctly to partial fills and netting behavior.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `MaximumRisk` | `0.02` | Fraction of portfolio equity risked on each entry. |
| `DecreaseFactor` | `3` | Divisor used to shrink position size after two or more consecutive losses. |
| `MovingPeriod` | `12` | Length of the simple moving average applied to closing prices. |
| `MovingShift` | `6` | Number of completed candles used to offset the SMA forward in time. |
| `CandleType` | `1h time frame` | Primary candle series processed by the strategy. |

## Usage Tips

1. Attach the strategy to a security and portfolio in StockSharp Designer or code.
2. Adjust the candle type to match the desired MetaTrader chart period.
3. Tune `MaximumRisk` and `DecreaseFactor` to match your account size and desired risk tolerance.
4. Run backtests to validate that the crossover signals align with the original MetaTrader results.
