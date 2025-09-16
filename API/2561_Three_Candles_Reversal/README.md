# Three Candles Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a faithful StockSharp port of the MQL5 expert advisor `Exp_ThreeCandles`. It looks for a classic three-candle reversal:

1. Two consecutive candles in one direction.
2. A third candle that flips the direction and closes beyond the middle bar.
3. Optional volume confirmation unless the oldest bar in the pattern is exceptionally large.

When a bullish configuration appears the algorithm closes short exposure and can enter a long position. A bearish configuration does the opposite. Protective stop-loss and take-profit levels are applied using the current instrument price step.

## Pattern detection

The strategy keeps a rolling window of the most recent `SignalBar + 3` finished candles. On every new bar it checks the candle at `SignalBar` offset (default: 1 bar back) and the three older candles:

- **Bullish reversal** (potential long):
  - The two older candles (`SignalBar + 3` and `SignalBar + 2`) are bearish.
  - The middle candle closes above the low of the oldest bar.
  - The most recent candle before the signal (`SignalBar + 1`) is bullish and closes above the open of the middle bar.
- **Bearish reversal** (potential short):
  - Mirror logic of the bullish case.

A volume filter mirrors the original indicator. The filter is skipped when `MaxBarSize` (in price steps) is exceeded by the oldest candle range or when `VolumeFilter` is set to `None`. Otherwise the reversal must satisfy `older volume < middle volume` **OR** `recent volume > middle volume` **OR** `recent volume > oldest volume`. Tick and real volume are mapped to the candle's aggregated volume because StockSharp does not distinguish the two in the high level candle stream.

## Trade management

- If `AllowSellExit` is enabled, a bullish pattern immediately covers any short position before considering a long entry. `AllowBuyExit` behaves the same for longs on bearish patterns.
- New positions are only opened when the current position is flat and the corresponding `Allow*Entry` flag is true. Order size uses the strategy's standard volume settings.
- Stop-loss and take-profit distances (`StopLossPips`, `TakeProfitPips`) are expressed in price steps and monitored on every finished candle.
- The last processed bullish/bearish signal time is cached to avoid duplicate actions while a candle keeps triggering ticks.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `CandleType` | 4 hour time frame | Candle series processed by the strategy. |
| `SignalBar` | 1 | How many bars back the signal is evaluated. Must be ≥ 0. |
| `MaxBarSize` | 300 | If the oldest bar range (converted with `PriceStep`) exceeds this value the volume filter is skipped. Set to 0 to always skip. |
| `VolumeFilter` | `Tick` | Volume mode (`Tick`, `Real`, or `None`). Both `Tick` and `Real` use `TotalVolume` from candles. |
| `AllowBuyEntry` | `true` | Enable long entries on bullish patterns. |
| `AllowSellEntry` | `true` | Enable short entries on bearish patterns. |
| `AllowBuyExit` | `true` | Allow closing long positions on bearish patterns. |
| `AllowSellExit` | `true` | Allow closing short positions on bullish patterns. |
| `StopLossPips` | 1000 | Stop-loss distance in price steps (0 disables). |
| `TakeProfitPips` | 2000 | Take-profit distance in price steps (0 disables). |

## Conversion notes

- Money-management routines from the original MQL5 include file were replaced by StockSharp's `BuyMarket`/`SellMarket` calls. Position size therefore follows the engine's default volume.
- Signal timing mirrors the expert advisor by evaluating the bar at `SignalBar` offset and keeping the previous signal timestamp.
- Email, push, and sound alerts from the MQL indicator are intentionally omitted.
- Volume modes are preserved but both map to the candle's aggregate volume because separate tick and real volumes are not available in the high-level API.
- All comments were rewritten in English as required by the project guidelines.

This implementation stays close to the original behaviour while adhering to StockSharp's high-level subscription model.
