# Ytg Multi Stoch Strategy

## Origin
- Converted from the MetaTrader 4 expert advisor `ytg_Multi_Stoch.mq4`.
- Original logic checks multiple Forex symbols at the close of every bar and trades whenever stochastic oscillators cross from extreme zones.
- The StockSharp port keeps the multi-symbol workflow and translates pip-based risk rules into decimal price offsets.

## Trading Logic
1. Monitor up to four user-selected securities on the same timeframe.
2. For each security, calculate a stochastic oscillator with parameters K=5, D=3, slowing=3 and a simple moving average smoothing method.
3. A **long** entry is triggered when:
   - Current %K is below 20 (oversold).
   - Previous %K was below previous %D (the fast line was underneath the signal line).
   - Current %K is now above current %D (bullish crossover).
4. A **short** entry is triggered when:
   - Current %K is above 80 (overbought).
   - Previous %K was above previous %D.
   - Current %K is now below current %D (bearish crossover).
5. Only one position per security is allowed. New signals are ignored while a position or pending protective target exists.
6. Upon entry the strategy records stop-loss and take-profit levels expressed in pips and turns them into absolute price levels using the instrument price step.
7. On every completed candle the stop-loss and take-profit are checked first; if either level is touched the position is closed at market and the internal state is reset.

## Risk and Trade Management
- **Stop-Loss**: configurable distance in pips subtracted from (long) or added to (short) the entry price.
- **Take-Profit**: configurable distance in pips added to (long) or subtracted from (short) the entry price.
- Both targets are optional; set them to zero to disable the corresponding protection.
- The pip size is automatically derived from `Security.PriceStep`. If the exchange reports fractional steps (e.g. 0.00001) the code scales them to the conventional pip (0.0001).

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `UseSymbol1..4` | Enable or disable each of the four instrument slots. | `true` |
| `Symbol1..4` | Securities traded in the enabled slots. | `null` |
| `TradeVolume` | Base order volume for new market entries. | `0.01` |
| `StopLossPips` | Stop-loss distance expressed in pips. | `50` |
| `TakeProfitPips` | Take-profit distance expressed in pips. | `10` |
| `CandleType` | Timeframe used for signal generation. | `30 minutes` |

## Multi-Instrument Handling
- The high-level API `SubscribeCandles` is used to register an independent candle subscription per enabled security.
- Each subscription is bound to its dedicated `StochasticOscillator` instance and processed via a shared callback.
- Position size and protective targets are tracked individually for every symbol using an internal context object.

## Usage Notes
- Assign up to four symbols via the strategy parameters before starting the strategy.
- Ensure the selected portfolio supports all chosen instruments; the strategy reads positions using `GetPositionValue` per security.
- Because the algorithm works on completed candles, attach it to a connector that can supply candle series (e.g., through `TimeFrameCandleBuilder`).
- The protective logic is evaluated before new entries, so tightening stop-loss or take-profit values will immediately affect open trades.

## Differences from the MQL Version
- The StockSharp port uses market orders with explicit volume while the original EA relied on `OrderSend` with broker-side stop/limit orders.
- Protective stops are emulated within the strategy loop instead of being attached to the order ticket. This keeps the behavior deterministic across different brokers.
- Additional guards ensure trading only occurs when the strategy is fully online and the connector confirms readiness.
