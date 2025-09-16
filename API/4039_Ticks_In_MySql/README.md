# Ticks In MySQL Strategy

## Overview
- Reimplementation of the MetaTrader 4 "TicksInMySQL" expert for the StockSharp framework.
- Connects to a MySQL database and writes every incoming level 1 quote together with current portfolio metrics.
- Stores margin usage, free margin, equity, bid/ask prices, and the instrument identifier without placing any orders.

The strategy is intended for environments where tick-by-tick account telemetry must be collected in an external database for later risk analysis or reporting. Trading is never initiated by this strategy; it only records information.

## MySQL requirements
- The sample relies on the [MySql.Data](https://www.nuget.org/packages/MySql.Data/) provider. Add the package to the host application that loads the strategy (Backtester, Designer, or your own project).
- The MySQL user must have permission to create tables (when auto creation is enabled) and to insert rows into the destination table.
- Network access from the strategy host to the MySQL server must be allowed.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `Server` | `localhost` | Host name or IP address of the MySQL server. |
| `Port` | `3306` | TCP port used to reach the server. |
| `Database` | `mt4` | Database that contains the destination table. |
| `User` | `user` | Login used for the connection. |
| `Password` | `pwd` | Password for the user (stored in plain text in settings). |
| `TableName` | `ticks` | Name of the table that will receive the data. |
| `AutoCreateTable` | `true` | Create the table on start if it does not exist. Requires DDL permissions. |
| `PricePrecision` | `4` | Number of decimal digits used when storing bid/ask values (mirrors `NormalizeDouble` from MQL). |

## Database schema
When `AutoCreateTable` is enabled the strategy issues the following statement (table name is substituted dynamically):

```sql
CREATE TABLE IF NOT EXISTS `ticks` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `margin` DECIMAL(19,6) NOT NULL,
    `freemargin` DECIMAL(19,6) NOT NULL,
    `date` DATETIME NOT NULL,
    `ask` DECIMAL(19,6) NOT NULL,
    `bid` DECIMAL(19,6) NOT NULL,
    `symbol` VARCHAR(64) NOT NULL,
    `equity` DECIMAL(19,6) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB;
```

Adjust column types if your installation requires different numeric precision. The insert command executed during runtime matches the column order above.

## Logged columns
Each accepted level 1 update results in an insert with the values below:

- `margin`: current value of `Portfolio.BlockedValue` (capital locked in orders/margin).
- `freemargin`: calculated as `Portfolio.CurrentValue - Portfolio.BlockedValue`.
- `date`: UTC timestamp of the quote (server time when available, otherwise current strategy time).
- `ask`: best ask rounded to `PricePrecision` decimals.
- `bid`: best bid rounded to `PricePrecision` decimals.
- `symbol`: `Security.Id` of the instrument served by the strategy.
- `equity`: portfolio equity (`Portfolio.CurrentValue`, falls back to `BeginValue` when the current value is unknown).

Rows are skipped until both bid and ask values are received at least once to avoid writing incomplete information.

## Usage workflow
1. Ensure the host solution references the MySql.Data package.
2. Provide valid MySQL credentials in the strategy parameters and assign a portfolio/security before starting.
3. (Optional) Enable `AutoCreateTable` for first-time initialization or create the table manually using the schema above.
4. Start the strategy; it subscribes to level 1 market data and begins writing rows immediately after the first complete quote.
5. Monitor the log for connection or insert errors (`LogError` entries). The strategy stops automatically when it fails to establish the connection during startup.

## Operational notes
- The strategy is single-threaded regarding database operations by protecting inserts with a synchronization lock.
- Password management is outside of the strategy scope; secure storage should be configured in the host application.
- No risk management or order logic is implemented. Combine with other strategies if trading is required in parallel.
- Consider adding indexes (for example, on the `date` column) when storing large datasets.
