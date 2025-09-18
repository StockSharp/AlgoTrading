# Dealers Trade MACD MQL4 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Dealers Trade MACD MQL4 strategy is a direct conversion of the "Dealers Trade v7.74" expert advisor for MetaTrader 4. It keeps the pyramiding money management and the MACD slope logic of the original system while adapting position handling to StockSharp's netted accounts. The strategy is designed for swing trading on H4/D1 charts and continuously adds to the trend as long as momentum remains aligned with the MACD main line.

## How the strategy works

- **Signal detection** – the strategy subscribes to candles of the configured timeframe and calculates a classic MACD indicator (fast EMA, slow EMA and signal EMA). A rising MACD main value compared to the previous bar signals bullish momentum, while a falling value signals bearish momentum. The `ReverseCondition` parameter can be used to flip the direction when a contrarian approach is preferred.
- **Order spacing and scaling** – only one directional basket is active at a time. When the MACD indicates a long trend, the strategy opens an initial market buy order. Additional buys are sent only when the price has moved down by at least `SpacingPips * PriceStep` from the last entry price, mirroring the "averaging" behaviour from the MQL script. Short baskets behave symmetrically when the MACD slope turns negative.
- **Lot sizing** – the base lot size is either the fixed `FixedVolume` or, if `UseRiskSizing` is enabled, a value derived from the portfolio equity and `RiskPercent`. Mini accounts are supported through the `IsStandardAccount` flag that emulates the original "Account is normal" option. Every extra order within the same basket is multiplied by `LotMultiplier` and capped by `MaxVolume`.
- **Risk controls** – hard stop loss and take profit levels are attached to each position using the `StopLossPips` and `TakeProfitPips` distances. Once a trade has moved by `TrailingStopPips + SpacingPips` in profit the stop level is tightened to keep at least `TrailingStopPips` of profit, reproducing the trailing rule from the MetaTrader implementation.
- **Account protection** – when the number of open trades reaches `MaxTrades - OrdersToProtect` and the aggregate unrealised profit exceeds `SecureProfit`, the most recent trade is closed to lock in gains before new orders are considered. This corresponds to the "AccountProtection" block in the source EA.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | H4 | Timeframe used for MACD calculations and signal evaluation. |
| `FixedVolume` | 0.1 | Base lot size when `UseRiskSizing` is disabled. |
| `UseRiskSizing` | true | Enables balance based position sizing. |
| `RiskPercent` | 2 | Percentage of equity used to size positions when `UseRiskSizing` is true. |
| `IsStandardAccount` | true | Set to false for mini accounts (lots divided by 10). |
| `MaxVolume` | 5 | Maximum volume allowed for a single order. |
| `LotMultiplier` | 1.5 | Multiplier applied to the base lot for each additional entry in the basket. |
| `MaxTrades` | 5 | Maximum number of simultaneously open trades. |
| `SpacingPips` | 4 | Minimum pip distance between consecutive entries. |
| `OrdersToProtect` | 3 | Number of orders kept before the protection block can open new trades. |
| `AccountProtection` | true | Enables the secure profit protection logic. |
| `SecureProfit` | 50 | Unrealised profit (in account currency) required to trigger protection. |
| `TakeProfitPips` | 30 | Take profit distance per trade, expressed in pips. |
| `StopLossPips` | 90 | Stop loss distance per trade, expressed in pips. |
| `TrailingStopPips` | 15 | Trailing stop distance applied after activation. |
| `ReverseCondition` | false | Inverts the MACD slope interpretation. |
| `MacdFast` | 14 | Fast EMA length for the MACD indicator. |
| `MacdSlow` | 26 | Slow EMA length for the MACD indicator. |
| `MacdSignal` | 1 | Signal EMA length for the MACD indicator. |

## Notes and limitations

- StockSharp strategies manage a net position per security, therefore hedged long and short baskets cannot coexist. The original EA allowed hedging but the conversion closes the opposite side before switching direction.
- The secure profit logic calculates unrealised profit using the instrument `PriceStep` and `StepPrice` metadata. Instruments without this information fallback to a nominal pip value of 0.0001 with a unit currency step, so adjust thresholds accordingly.
- Risk based sizing requires a positive `StopLossPips` value. When the stop distance is zero the calculated risk amount becomes undefined and the strategy will skip trading.
- The strategy works on closed candles only. Signals that relied on intrabar MACD movements in MetaTrader may appear a bar later in this implementation, but the behaviour is significantly more stable for backtesting.
