# Auto SLTP Manager Strategy

## Overview
The **Auto SLTP Manager Strategy** ports the MetaTrader 5 expert advisor "AutoSLTP" to StockSharp. It continuously synchronizes
stop-loss and take-profit levels for the currently traded instrument using a text configuration file. The strategy does not
produce entry signals; instead, it manages existing positions opened manually or by another strategy. When a position is open, it
tracks the latest trade price and moves protective levels closer to the price according to the rules defined in the
configuration file. If price reaches either the trailing stop or trailing target, the strategy exits the position with a market
order.

## Key Features
- **External configuration** – reads symbol specific rules from a text file located outside of the compiled strategy.
- **Side dependent distances** – allows different stop-loss and take-profit distances for long and short positions.
- **Automatic pip conversion** – converts pip distances to price offsets by using the instrument `PriceStep` and applying the
  3/5-digit correction used in the original MQL version.
- **Timed updates** – recalculates protective levels no more often than once every `UpdateInterval` to avoid excessive order
  modifications.
- **Safe guards** – ignores malformed records, validates numeric fields, and stops with descriptive errors if no matching symbol
  configuration is found.

## Configuration File
The configuration file is specified by the `ConfigurationPath` parameter. Each non-empty line must follow the
`symbol*position_type*sl*tp` format, where:

| Field            | Description                                                                                   |
|------------------|-----------------------------------------------------------------------------------------------|
| `symbol`         | Symbol identifier. It may match either `Security.Id` (e.g., `EURUSD@FXCM`) or `Security.Code` (e.g., `EURUSD`). |
| `position_type`  | Either `POSITION_TYPE_BUY` for long rules or `POSITION_TYPE_SELL` for short rules.            |
| `sl`             | Stop-loss distance in pips. Must be a positive integer.                                       |
| `tp`             | Take-profit distance in pips. Must be a positive integer.                                     |

Example (`AutoSLTP/AutoSLTP.txt`):

```
EURUSD*POSITION_TYPE_BUY*50*96
EURUSD*POSITION_TYPE_SELL*40*46
```

Comments starting with `#` and blank lines are ignored. The strategy only loads entries for the security attached to the
strategy instance; other lines remain untouched, enabling a single file to hold settings for many instruments.

## Parameters
| Name                | Type      | Default value                 | Description                                                                                   |
|---------------------|-----------|-------------------------------|-----------------------------------------------------------------------------------------------|
| `ConfigurationPath` | `string`  | `AutoSLTP/AutoSLTP.txt`       | Relative or absolute path to the configuration file.                                          |
| `UpdateInterval`    | `TimeSpan`| `00:00:10` (10 seconds)       | Minimum delay between successive trailing recalculations.                                     |

Both parameters can be changed at runtime or exposed to optimizers. The configuration path is resolved via
`Path.GetFullPath`, so relative paths are evaluated against the application working directory.

## Trailing Logic
1. When a new trade message arrives for the security, the strategy verifies that enough time has passed since the last update.
2. If a long position is active, it checks stored stop-loss and take-profit levels:
   - If price falls to the stop-loss level, the strategy closes the long position with `SellMarket` and resets internal state.
   - If price rallies to the take-profit level, it also closes the position via `SellMarket`.
3. If neither exit triggers, the strategy raises the trailing stop (`price - sl_distance`) and the trailing target
   (`price + tp_distance`) whenever the new price produces more favourable values.
4. For short positions, the algorithm mirrors the logic using symmetric price comparisons and `BuyMarket` orders.
5. When no position is active, stored levels are cleared so the next entry begins from the new price.

The distances expressed in pips are transformed into price offsets using the exchange-provided `PriceStep`. When the step
contains three or five decimal places, the value is multiplied by ten to mimic the "5-digit" correction from MetaTrader.

## Differences from the MQL Expert Advisor
- The StockSharp version uses market exits instead of modifying broker-side stop-loss/take-profit values. This guarantees that
  protective logic works even with connectors that do not support native stop modifications.
- Errors in the configuration file stop the strategy during start-up with descriptive exceptions instead of silently printing to
  the MetaTrader log.
- The strategy relies on aggregated trade data obtained via `SubscribeTrades()` rather than polling positions every 10 seconds.
- Once the configuration is loaded the strategy manages only the security it is attached to, which is consistent with
  StockSharp’s single-security strategy model.

## Usage Steps
1. Create a text file using the required format and place it at the location referenced by `ConfigurationPath`.
2. Attach the strategy to a connector and security, then start it together with any entry strategy you want to protect.
3. Open positions manually or through automated logic; the Auto SLTP Manager will immediately begin trailing the protective
   levels according to the specified distances.
4. Adjust `UpdateInterval` when faster or slower reaction is needed. Setting it too low may create redundant orders, while high
   values reduce responsiveness.
5. Monitor the strategy log for configuration issues. When a security entry is missing, the strategy stops with a clear message
   so the operator can fix the configuration file.

## Best Practices
- Keep the configuration file under version control to track historical changes to risk limits.
- Use different `AutoSltpManagerStrategy` instances when multiple securities require independent trailing policies.
- Combine the manager with StockSharp’s `StartProtection` feature only if protective levels do not overlap; otherwise prefer a
  single mechanism to avoid conflicting exits.
- Backtest by feeding historical trades or candles that approximate execution prices so the trailing logic can be validated
  before running live.

## Limitations
- The strategy does not open trades; it expects external logic to handle entries.
- Managing portfolios with many symbols requires either multiple strategy instances or a custom wrapper that instantiates
  several managers, one per security.
- Trailing calculations depend on incoming trade messages; if a connector provides sparse tick data the reaction time may be
  slower than the configured `UpdateInterval`.

