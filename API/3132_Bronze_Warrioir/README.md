# Bronze Warrioir Strategy

## Overview
- Conversion of the MetaTrader 5 expert *Bronze Warrioir.mq5* into the StockSharp high level API.
- Trades a single symbol using finished candles and combines CCI, Williams %R and a proprietary "DayImpuls" oscillator.
- Focused on catching momentum bursts that occur when the DayImpuls slope, Williams %R extremes and CCI readings align.

## Indicator Stack
- **Commodity Channel Index (CCI)** – classic CCI using the configured `IndicatorPeriod`. Long signals require the value to be below `-CciLevel`, short signals need it above `CciLevel`.
- **Williams %R** – applied on the same period. A value above `WilliamsLevelUp` confirms overbought territory, while values below `WilliamsLevelDown` confirm oversold levels.
- **DayImpuls oscillator** – replica of the bundled custom indicator. It converts every candle body into points (close minus open divided by the instrument point value) and applies two consecutive exponential moving averages with the same period. Rising values indicate growing bullish pressure; falling values indicate bearish pressure.

## Trading Logic
1. **Equity protection** – before generating any signals the strategy accumulates the floating PnL of the current exposure. If it rises above `ProfitTarget` or drops below `LossTarget`, all open positions are closed immediately.
2. **Entry filter** – finished candles are mandatory. The algorithm requires a stored DayImpuls value from the previous bar to emulate the original look-back using `custom[1]`.
3. **Short setup** – triggered when:
   - There is no active short exposure.
   - DayImpuls is above `DayImpulsLevel` and larger than its previous value (positive momentum).
   - Williams %R is above `WilliamsLevelUp` (overbought) and CCI is greater than `CciLevel`.
   - Orders use `TradeVolume` plus any open long volume to reverse in a single transaction inside the StockSharp netting model.
4. **Long setup** – symmetric conditions:
   - No active long exposure.
   - DayImpuls is below `DayImpulsLevel` and smaller than its previous value (falling momentum).
   - Williams %R is below `WilliamsLevelDown` and CCI is less than `-CciLevel`.
   - Uses `TradeVolume` plus any outstanding short volume for a full reversal when needed.
5. **Hedge-style reversals** – when only one directional exposure is present and the floating PnL leaves the band `[-PredTarget / 2, PredTarget]`, the EA validated the martingale step through the `LotCoefficient` parameter. In the StockSharp port the validation is preserved but the actual execution performs a close-and-reverse order because the platform keeps net positions instead of independent hedged tickets.

## Risk Management
- `StopLossPips` and `TakeProfitPips` are converted into price distances using the instrument `PriceStep`. For 3 or 5 digit forex symbols an extra factor of 10 is applied to emulate MetaTrader "pips".
- Both values are passed to the high-level `StartProtection` helper which attaches automated stop-loss and take-profit levels to the active position.
- The strategy maintains internal long/short volume tracking so that `GetOpenPnL` matches the MetaTrader calculation that sums `Commission + Swap + Profit` for each ticket.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Base order volume in lots. | `1` |
| `StopLossPips` | Protective stop in pips converted to price distance. | `50` |
| `TakeProfitPips` | Profit target in pips converted to price distance. | `50` |
| `IndicatorPeriod` | Period applied to CCI, Williams %R and DayImpuls. | `14` |
| `CciLevel` | Absolute CCI threshold for trades. | `150` |
| `WilliamsLevelUp` | Williams %R overbought level (negative value). | `-15` |
| `WilliamsLevelDown` | Williams %R oversold level (negative value). | `-85` |
| `DayImpulsLevel` | DayImpuls threshold separating bullish/bearish regimes. | `50` |
| `ProfitTarget` | Floating profit target in account currency. | `100` |
| `LossTarget` | Floating loss limit in account currency. | `-100` |
| `PredTarget` | Band used to trigger averaging reversals. | `40` |
| `LotCoefficient` | Validation coefficient inherited from the EA. | `2` |
| `CandleType` | Time-frame used for all indicators. | `15m` candles |

## Implementation Notes
- The DayImpuls oscillator is embedded as an inner indicator class and mirrors the original double EMA smoothing logic.
- Because StockSharp strategies manage net positions, simultaneous long/short hedges from the MQL version are emulated by combining closing and opening volumes inside the same market order.
- The strategy only works with finished candles and uses `IsFormedAndOnlineAndAllowTrading()` to respect the global strategy lifecycle.
- Long/short average prices are tracked through `OnOwnTradeReceived` so that partial closes and reversals update the floating PnL correctly.
