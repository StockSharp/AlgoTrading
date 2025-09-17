# ABE BE Stochastic Engulfing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the MetaTrader Expert Advisor **Expert_ABE_BE_Stoch** to the StockSharp high-level API. It mixes Japanese candlestick analysis with momentum confirmation to time reversals around oversold and overbought zones. The primary signal looks for a bullish engulfing candle backed by a deeply oversold stochastic oscillator, or a bearish engulfing candle confirmed by an overbought oscillator reading. Once a position is open, the strategy relies on stochastic threshold crosses to manage exits, replicating the "vote" mechanics of the original expert.

The tactic is designed for both long and short participation. It evaluates only completed candles and therefore stays immune to intrabar noise. Trade sizing remains under the control of the framework's `Volume` property, while optional stop-loss and take-profit protections convert the original point-based risk settings into StockSharp `Unit` objects.

## How it works

1. **Data subscription** – The strategy subscribes to the configured candle type and builds a `StochasticOscillator` with three tunable parameters (`%K`, `%D`, and the slowing factor).
2. **Pattern detection** – On every finished candle the algorithm checks whether the latest bar engulfs the body of the previous one. Two helper methods reproduce bullish and bearish engulfing definitions used in MetaTrader.
3. **Momentum confirmation** – The `%D` line of the stochastic serves as the confirmation filter. Values below the oversold threshold (default 30) are required for bullish engulfing trades, while values above the overbought threshold (default 70) are required for bearish signals.
4. **Position management** – The previous `%D` value is cached. If the new reading crosses upward through either 20 or 80, any short exposure is closed. Conversely, downward crosses through 80 or 20 liquidate long exposure. These thresholds mirror the additional "close" votes produced by the MQL logic.
5. **Risk handling** – When positive stop-loss or take-profit distances (expressed in price steps) are supplied, the strategy converts them to `UnitTypes.Price` and enables `StartProtection`. Otherwise the default StockSharp protection is activated with `StartProtection()`.

## Trading rules

- **Long entry**: Previous candle is bearish, current candle is bullish, and the current candle's body engulfs the previous body. The stochastic `%D` value must be below the `EntryOversoldLevel` (default 30). Any existing short is closed and a new long is opened via `BuyMarket`.
- **Short entry**: Previous candle is bullish, current candle is bearish, and the current candle's body engulfs the previous body. The stochastic `%D` value must exceed the `EntryOverboughtLevel` (default 70). Any existing long is closed and a new short is opened via `SellMarket`.
- **Long exit**: With an open long, if `%D` crosses downward through either `ExitUpperLevel` (default 80) or `ExitLowerLevel` (default 20), the position is closed with `SellMarket`.
- **Short exit**: With an open short, if `%D` crosses upward through either `ExitLowerLevel` or `ExitUpperLevel`, the position is covered using `BuyMarket`.
- **Stops/targets**: Optional `StopLossPoints` and `TakeProfitPoints` convert point-based distances to absolute price offsets when the instrument exposes a non-zero `PriceStep`.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Candle source used for pattern detection. |
| `StochasticPeriodK` | `int` | `47` | Lookback period for the fast `%K` calculation. |
| `StochasticPeriodD` | `int` | `9` | Smoothing period for the `%D` signal line. |
| `StochasticPeriodSlow` | `int` | `13` | Additional smoothing applied to `%K` before it becomes `%D`. |
| `EntryOversoldLevel` | `decimal` | `30` | Upper bound for `%D` that allows bullish engulfing trades. |
| `EntryOverboughtLevel` | `decimal` | `70` | Lower bound for `%D` that allows bearish engulfing trades. |
| `ExitLowerLevel` | `decimal` | `20` | Level that, when crossed upward, forces short exits; when crossed downward, it closes longs. |
| `ExitUpperLevel` | `decimal` | `80` | Upper boundary used in the same way as the lower level but for overbought territory. |
| `TakeProfitPoints` | `decimal` | `0` | Distance in price steps for the take-profit order (0 disables it). |
| `StopLossPoints` | `decimal` | `0` | Distance in price steps for the stop-loss order (0 disables it). |

## Notes

- Works on any instrument that supplies OHLC candles; defaults assume hourly bars.
- All calculations rely on closed candles to stay aligned with the MQL expert's timeframe logic.
- Position size should be configured through the base strategy `Volume` property or higher-level portfolio management.
