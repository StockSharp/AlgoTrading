# Futures Portfolio Control Expiration Strategy

## Overview
This strategy rebuilds the MetaTrader 5 expert advisor *Futures Portfolio Control Expiration* on top of the StockSharp high-level API. It maintains a three-leg futures portfolio, keeps the desired long/short exposure for each leg, and automatically rolls every contract to the next expiry when the remaining lifetime drops below a configurable threshold.

The implementation replicates the original workflow:
1. Identify the currently tradable contract for each futures family based on a short code (for example `MXI` or `BR`).
2. Open or adjust the position so that the actual portfolio volume matches the configured lot value (positive = long, negative = short).
3. Monitor the expiry time on every finished candle of a heartbeat subscription.
4. Close the expiring contract, discover the next expiry in the same family, and recreate the target exposure on the new contract.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `BoardCode` | Exchange board appended to futures identifiers (for example `FORTS`). Leave empty if the provider does not require a board suffix. | `FORTS` |
| `Symbol1`, `Symbol2`, `Symbol3` | Short codes of the three futures families. The strategy iterates future expiries by constructing identifiers like `CODE-M.YY`. | `MXI`, `BR`, `SBRF` |
| `Lot1`, `Lot2`, `Lot3` | Target position size per leg. Positive values create long exposure, negative values create short exposure. | `-4`, `-1`, `5` |
| `HoursBeforeExpiration` | Number of hours before contract expiration when the roll should start. | `25` |
| `MonitoringCandleType` | Candle type used only as a heartbeat to trigger expiration checks (for example hourly candles). | `1H` timeframe |

## Rolling and position management
- **Contract discovery.** For each leg the strategy scans up to twelve consecutive calendar months. It tries multiple identifier formats (`CODE-M.YY`, `CODE-MM.YY`, `CODEMMYY`, `CODEMYY`) and optionally appends the configured `BoardCode`. Only securities with an expiration date later than the reference time are eligible.
- **Heartbeat updates.** A candle subscription on each active contract provides a finished-candle callback that re-evaluates expiration timers and synchronises the portfolio exposure.
- **Rolling logic.** When the remaining lifetime is less than or equal to `HoursBeforeExpiration`, the strategy closes any open position on the current contract, locates the next future with a later expiration, re-subscribes to heartbeat candles, and restores the target lot on the new contract.
- **Position synchronisation.** After every heartbeat the actual position is compared against the target lot. The strategy increases or decreases exposure with market orders so that the live position always matches the requested volume (including zero).

## Usage notes
1. Ensure the `SecurityProvider` knows all future symbols for the selected families. Configure `BoardCode` if your data source requires identifiers like `Si-9.23@FORTS`.
2. Start the strategy with the desired portfolio parameters. Positions are opened only when the strategy is online and trading is allowed.
3. The strategy logs every assignment, adjustment, and roll event. Use these messages to verify the mapping between short codes and actual futures.
4. Because the heartbeat subscription is only a timer, you can choose any candle type that is consistently available for the traded instruments.

## Implementation details
- High-level API components (`SubscribeCandles`, `StrategyParam`, `BuyMarket`/`SellMarket`) keep the code concise and adhere to the project guidelines.
- No custom collections of historical data are stored; the strategy only works with the latest candle event and the position state.
- English comments inside the code describe every important step for easier maintenance.
