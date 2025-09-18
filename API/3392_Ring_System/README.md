# Ring System Strategy

## Concept

The **Ring System Strategy** recreates the triangular arbitrage core idea from the "MT5 Ring System EA" inside StockSharp. The
strategy monitors three currency pairs that form a closed conversion ring (e.g. `EURUSD`, `USDJPY`, `EURJPY`). Whenever the prod
uct of the first two legs deviates from the direct cross, the strategy opens a balanced basket to exploit the imbalance and clu
oses the basket once the prices revert.

The implementation focuses on transparency and makes extensive use of the high-level candle subscription API. Each leg is monit
ored independently and trades are executed using synchronized market orders so that the net exposure of the ring remains close t
o neutral.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Currencies` | Ordered list of currencies used to build the ring. Only the first three entries are used. |
| `Entry Threshold` | Minimum relative deviation between the synthetic price (`leg1 * leg2`) and the direct cross required to open a ring trade. |
| `Exit Threshold` | Deviation level that triggers the exit of an opened ring. Usually lower than the entry threshold. |
| `Order Volume` | Volume sent to each leg when building or flattening the ring. |
| `Candle Type` | Candle aggregation used to evaluate spreads. Any timeframe supported by the data feed can be selected. |
| `Symbol Prefix` | Optional prefix added in front of every generated symbol. Useful when the data provider requires prefixes like `m.` or `FX_`. |
| `Symbol Suffix` | Optional suffix appended to every generated symbol. Use it for brokers that expose symbols such as `EURUSD.i`. |
| `Flatten On Stop` | When enabled the strategy closes all legs automatically after it is stopped. |

## Trading Logic

1. Parse the currency string, keep the first three tickers and build the three symbols of the triangular ring.
2. Subscribe to candles for each leg and store the last closing price once a candle is finished.
3. Compute the theoretical cross (`price1 * price2`) and compare it with the direct cross (`price3`).
4. When the relative deviation exceeds the entry threshold, open the basket:
   - Long ring: buy the direct cross (leg 3) and short the two synthetic legs.
   - Short ring: sell the direct cross and buy the remaining legs.
5. Once the absolute deviation falls below the exit threshold, close all open positions.
6. Optionally flatten every leg during the `OnStopped` stage for additional safety.

## Usage Notes

- The strategy relies on the three securities being available from the security provider. Use the prefix and suffix parameters w
hen required by the broker naming convention.
- All trades are executed with market orders to replicate the immediate execution approach of the original EA.
- The logic intentionally avoids stacking multiple baskets at the same time. New trades are only opened if every leg is flat.
- Only one triangular ring is supported per strategy instance. Launch multiple instances to trade additional currency groups.

## Differences Compared to the Original EA

- The StockSharp conversion focuses on a single triangular ring, while the MT5 expert can manage up to 56 rings at once.
- Money management features (automatic lot size, progressive steps, etc.) are replaced by the simpler `Order Volume` parameter.
- The visual dashboard and persistent logging options available in the MQL version are not part of this port.
- Trading session restrictions and slippage checks from the original EA are not implemented. Use StockSharp risk controls if nee
ded.

## Backtesting

To backtest the strategy, configure three forex instruments that share a common base currency set and provide synchronized cancl
e data. Adjust the entry and exit thresholds according to the volatility of the selected pairs.
