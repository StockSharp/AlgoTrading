# Exp TimeZone Pivots Open System Tm Plus Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a high level StockSharp port of the **Exp_TimeZonePivotsOpenSystem_Tm_Plus** expert advisor. It recreates the proprietary *TimeZonePivotsOpenSystem* indicator that projects two breakout zones around the daily session open and trades the pullbacks that follow a breakout. Every component from the original script—signal delay, time filter, asymmetric exit logic and the money-management presets—was mapped to explicit parameters so the behaviour stays consistent with the MQL5 implementation.

## Trading logic

1. At the configured `StartHour` the strategy records the session open price. Two dynamic levels are then drawn at `OffsetPoints` (in points) above and below that anchor.
2. Whenever a finished candle closes **above** the upper level the strategy:
   - Schedules a long entry to be executed on the next candle (respecting the `SignalBar` delay) only if the current bar is no longer above the band.
   - Closes any open short position immediately if `SellPosClose` is enabled.
3. Whenever a finished candle closes **below** the lower level the strategy:
   - Schedules a short entry for the next candle provided the current bar is no longer below the band.
   - Closes any open long position immediately if `BuyPosClose` is enabled.
4. Entries are executed on the first update of the next candle thanks to `TryExecutePendingEntries`. This matches the original expert that delays the order until the new bar begins.

The signal delay parameter `SignalBar` reproduces the original `CopyBuffer` shift. A value of `0` reacts to the most recent closed bar, while `1` waits an extra bar before acting, giving additional confirmation.

## Order management

* **Stop-loss / take-profit** – The distances are set in points (`StopLossPoints`, `TakeProfitPoints`) and converted to price using the instrument’s step. Both levels are monitored using candle extremes so intrabar touches trigger an exit.
* **Time based exit** – When `TimeTrade` is true the position is force-closed after `HoldingMinutes` minutes, mirroring the `nTime` timer from the MQL5 code.
* **Manual closes** – Breakout signals of the opposite direction close the running trade if the corresponding `BuyPosClose` or `SellPosClose` flag is enabled.

## Money management

The `MoneyMode` parameter reproduces the `MarginMode` enumeration:

- `Lot` – fixed volume equal to `MoneyManagement`.
- `Balance` and `FreeMargin` – use account equity or free margin multiples (`MoneyManagement * equity / price`).
- `LossBalance` and `LossFreeMargin` – risk-based sizing that divides the desired capital fraction by the stop distance.

If `StopLossPoints` is set to zero the risk modes gracefully fall back to price-based sizing.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `MoneyManagement` | Base coefficient used to size the position depending on `MoneyMode`. | `0.1` |
| `MoneyMode` | Position sizing model (`Lot`, `Balance`, `FreeMargin`, `LossBalance`, `LossFreeMargin`). | `Lot` |
| `StopLossPoints` | Stop-loss distance expressed in points from the fill price. | `1000` |
| `TakeProfitPoints` | Take-profit distance expressed in points from the fill price. | `2000` |
| `DeviationPoints` | Informational parameter kept from the expert (slippage setting in points). | `10` |
| `BuyPosOpen` / `SellPosOpen` | Enable or disable long and short entries. | `true` |
| `BuyPosClose` / `SellPosClose` | Allow the opposite breakout to force-close positions. | `true` |
| `TimeTrade` | Enable the maximum holding time filter. | `true` |
| `HoldingMinutes` | Maximum position lifetime in minutes. | `720` |
| `OffsetPoints` | Distance of the pivot bands from the session open in points. | `200` |
| `SignalBar` | Number of bars to delay signal evaluation (0 = last closed bar). | `1` |
| `CandleType` | Main timeframe used to calculate the indicator. | `TimeSpan.FromHours(1).TimeFrame()` |
| `StartHour` | Hour of the day (0-23) that defines the session open price. | `0` |

## Usage notes

- The strategy assumes the security provides a valid `PriceStep`. If the instrument lacks that metadata a fallback of `0.0001` is used.
- Because entries are triggered on the first update of a new candle, the actual fill price will follow the market at that moment, just like the expert, which may differ from the theoretical open price in fast markets.
- To replicate the original indicator overlay, keep the backtest timeframe at or below H1 as the MQL5 script only operates on hourly or lower periods.
- Set `SignalBar` to `0` for more responsive behaviour or to `1` (the default) to wait for one extra bar after a breakout.

