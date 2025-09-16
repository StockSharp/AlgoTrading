# Martin 1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Conversion of the MetaTrader 5 expert advisor "Martin 1" into the StockSharp high level strategy API. The algorithm continuously maintains exposure and uses hedging-style martingale steps to recover drawdowns while pyramiding into profitable trends.

## Trading Logic

1. **Initial exposure** – when the strategy is flat it immediately opens a position in the direction defined by `StartDirection`, regardless of the time filter. The base order size is taken from `InitialVolume` after rounding to the instrument volume step.
2. **Time window filter** – when `UseTradingHours` is enabled, only scaling actions (pyramiding or hedging) are allowed between `StartHour` and `EndHour` inclusive, using the exchange time contained in candle timestamps.
3. **Pyramiding winners** – every open position is evaluated on each finished candle. If the floating profit of a long position exceeds the take-profit distance and remains positive, an additional long order with the current volume is sent. Short positions behave symmetrically. The new order price is assumed to be the close of the current candle.
4. **Hedging martingale** – when the start direction is long and a long position loses more than `(StopLossPips × pip size × (multiplication index + 1))`, the strategy opens an opposite short order. Before placing the hedge the volume is multiplied by `LotMultiplier`, rounded to the allowed step, and the multiplication counter is increased. The same logic is applied in reverse for short start direction. Hedging stops once `MaxMultiplications` steps have been reached.
5. **Global profit target** – the unrealized profit across all remaining positions (converted to money using `PriceStep`/`StepPrice`) is summed. If it exceeds `MinProfit`, every open position is closed by issuing a market order in the opposite direction, and the martingale state is reset.

## Risk and Money Management

- The pip size is computed from the security price step. Three- and five-digit quotes multiply the step by ten to emulate the original MetaTrader pip adjustment.
- Volumes are rounded down to the nearest `VolumeStep`. If the rounded value falls below the step, the order is skipped.
- The martingale counter and current volume are reset whenever the book becomes flat, either naturally or after hitting the global profit target.
- Profit estimation ignores commissions and swaps, mirroring the behaviour of the original script which relied purely on floating PnL.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle type that drives all calculations. | 1 minute timeframe |
| `UseTradingHours` | Enables or disables the time window filter. | `true` |
| `StartHour` | Inclusive hour when the time filter allows new scaling actions. | 2 |
| `EndHour` | Inclusive hour when scaling actions stop. | 21 |
| `LotMultiplier` | Factor applied to the current volume before opening a hedge. | 1.6 |
| `MaxMultiplications` | Maximum number of hedging steps that may be triggered. | 5 |
| `StartDirection` | Direction of the very first order after the strategy becomes flat. | Buy |
| `MinProfit` | Floating profit (in money) that forces all positions to close. | 1.5 |
| `InitialVolume` | Base volume for the very first order and reset state. | 0.1 |
| `StopLossPips` | Pip distance that triggers the next martingale hedge. | 40 |
| `TakeProfitPips` | Pip distance that triggers a pyramiding entry. | 100 |

## Implementation Notes

- `ProcessCandle` uses the high-level candle subscription pipeline (`SubscribeCandles().Bind(...)`) and operates strictly on finished candles, complying with the platform guidelines.
- Hedged exposure is tracked internally with two FIFO lists so that the strategy can emulate MetaTrader hedging behaviour even on netting accounts.
- Profit conversion relies on `Security.PriceStep` and `Security.StepPrice`. When those values are unavailable the difference in price is multiplied directly by the traded volume as a fallback.
- The strategy keeps trading continuously; disabling the time filter or setting wide hours will make the algorithm behave like the original always-on expert advisor.
