# Zone Recovery Button Strategy

The **Zone Recovery Button Strategy** is a direct conversion of the MetaTrader expert advisor "ZONE RECOVERY BUTTON VER1" (`MQL/25347`).
The original robot relied on on-chart BUY/SELL buttons to start a hedged basket. In this StockSharp port the manual panel is
replaced with parameters while the recovery logic, money/percent take-profits, trailing stop in currency and equity-stop
protection are preserved.

Once the strategy receives a starting direction it opens an initial market order. Whenever price travels across the configured
zone width, the system stacks an opposite trade with an increased volume. The basket is closed when the reference take-profit is
hit, the floating profit reaches the configured monetary/percentage target, the trailing stop gives back too much profit, or the
equity-stop threshold is violated.

## Trading rules

1. **Start direction** – emulates pressing the BUY or SELL button. The strategy opens the first order immediately once it receives
data and is allowed to trade. After closing the basket it can automatically restart with the same direction.
2. **Zone recovery** – on every recovery step the algorithm alternates direction. For long cycles it sells once price drops below
`Base Price - Zone Width`, then buys again when the market returns above the base. For short cycles the logic is mirrored.
3. **Volume scaling** – each additional hedge either multiplies the previous volume or adds a fixed increment, reproducing the
"Lots"/"Multiply" settings of the EA.
4. **Take-profit controls** – the basket is closed by:
   - pip-based take-profit measured from the reference price;
   - money target in account currency;
   - percent target calculated from the current portfolio value;
   - trailing logic that locks gains once the floating profit exceeds a threshold and then gives back more than the allowed drawdown;
   - emergency equity-stop that compares the current floating loss against the highest observed equity during the cycle.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromMinutes(5)` | Candle type used for monitoring price moves. |
| `StartDirection` | `Buy` | Initial cycle direction (BUY/SELL/NONE). |
| `AutoRestart` | `true` | Restart a new cycle automatically after the previous basket closes. |
| `TakeProfitPips` | `200` | Pip distance between the base price and the pip take-profit target. |
| `ZoneRecoveryPips` | `10` | Pip distance that triggers the next hedge in the opposite direction. |
| `InitialVolume` | `0.01` | Volume (lots) of the first trade. |
| `UseVolumeMultiplier` | `true` | If enabled, each hedge multiplies the previous volume; otherwise the `VolumeIncrement` is added. |
| `VolumeMultiplier` | `2` | Multiplier applied when `UseVolumeMultiplier` is `true`. |
| `VolumeIncrement` | `0.01` | Volume increment when `UseVolumeMultiplier` is `false`. |
| `MaxTrades` | `100` | Maximum number of trades in the basket. |
| `UseMoneyTakeProfit` | `false` | Enable closing when floating profit exceeds `MoneyTakeProfit`. |
| `MoneyTakeProfit` | `40` | Profit target in account currency. |
| `UsePercentTakeProfit` | `false` | Enable closing when floating profit exceeds `PercentTakeProfit` percent of balance. |
| `PercentTakeProfit` | `10` | Profit target in percent of the current portfolio value. |
| `EnableTrailing` | `true` | Enable trailing profit in currency. |
| `TrailingProfitThreshold` | `40` | Profit level that activates trailing. |
| `TrailingDrawdown` | `10` | Allowed drawdown from the peak floating profit before closing the basket. |
| `UseEquityStop` | `true` | Enable the emergency equity stop. |
| `TotalEquityRiskPercent` | `1` | Maximum floating loss (in percent of equity high) before flattening. |

## Notes

- The strategy works with any instrument that provides `PriceStep` and `StepPrice` values. These parameters are required to convert
pip distances to price and currency units.
- Because StockSharp uses a net position model, the hedging grid is simulated internally. The strategy keeps its own list of trade
steps to reproduce the MetaTrader profit calculation.
- The trailing logic operates on floating profit of the active basket. It does not use order-based trailing stops.
