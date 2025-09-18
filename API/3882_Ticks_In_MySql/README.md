# Ticks In MySQL Recorder Strategy

## Overview
- Port of the MetaTrader 4 expert `TicksInMySQL.mq4` that was stored in `MQL/7850`.
- Connects to a MySQL database and inserts a row for every level 1 update (tick) received by the strategy.
- Logs portfolio margin usage, free margin, equity, bid/ask prices, and the instrument identifier without placing any orders.
- Adds optional guards such as automatic table creation and a switch that stops the strategy when a database insert fails.

The implementation is intentionally passive: it never sends trading orders. Its sole purpose is to mirror the telemetry stream produced by the original EA so that the collected data can be used for reporting or risk analysis outside of StockSharp.

## MySQL requirements
- The sample relies on the [MySql.Data](https://www.nuget.org/packages/MySql.Data/) provider. Add the package to the host project (Designer, Backtester, Shell, or a custom runner) before compiling the strategy.
- The configured user must have permission to insert rows into the destination table. Enable `EnsureTableExists` only when the user can execute `CREATE TABLE` statements.
- The host application must be able to reach the MySQL server over the network. Firewalls or VPNs need to be configured accordingly.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `Server` | `localhost` | Host name or IP address of the MySQL server. |
| `Port` | `3306` | TCP port used for the connection. |
| `Database` | `mt4` | Database that contains the destination table. |
| `User` | `user` | Login used for authentication. |
| `Password` | `pwd` | Password for the user (stored in plain text in the strategy settings). |
| `TableName` | `ticks` | Name of the table that will receive the rows. |
| `EnsureTableExists` | `false` | Create the table on start when it does not exist yet. Requires DDL privileges. |
| `PricePrecision` | `4` | Number of decimal digits stored for bid/ask values (equivalent to `NormalizeDouble` in MQL). |
| `StopOnInsertError` | `false` | When enabled the strategy stops itself after logging an insert exception. |

## Database schema
When `EnsureTableExists` is enabled the strategy executes the following statement (table name is substituted dynamically):

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

Adjust column types if your deployment requires different precision. The insert command executed during runtime follows the same column order.

## Logged columns
Every finished update is stored with the values below:

- `margin`: current portfolio margin usage (`Portfolio.BlockedValue`).
- `freemargin`: `Portfolio.CurrentValue - Portfolio.BlockedValue`; mirrors `AccountFreeMargin()` from MQL.
- `date`: UTC timestamp of the quote (server time when available, otherwise `CurrentTime`).
- `ask`: best ask rounded to `PricePrecision` decimals.
- `bid`: best bid rounded to `PricePrecision` decimals.
- `symbol`: `Security.Id` of the instrument served by the strategy.
- `equity`: portfolio equity (`Portfolio.CurrentValue` with a fallback to `Portfolio.BeginValue`).

Rows are skipped until both bid and ask are known at least once to avoid writing incomplete information.

## Usage workflow
1. Reference the MySql.Data package inside the host solution.
2. Configure valid credentials in the strategy parameters and assign a security/portfolio before starting.
3. (Optional) Enable `EnsureTableExists` or create the table manually using the schema above.
4. Start the strategy; it subscribes to level 1 market data via `SubscribeLevel1()` and writes a row for every tick.
5. Monitor the log for errors. When `StopOnInsertError` is `true` the strategy will stop automatically after the first insert failure, mimicking the "fail fast" mode that is sometimes required for telemetry collectors.

## Operational notes
- The insert command is protected by a lock to keep database interactions single-threaded, matching the sequential nature of the MQL expert.
- Password storage is handled by StockSharp. Use external vaults or encrypted configuration if sensitive credentials must be protected.
- No trading logic is implemented; the strategy can run side-by-side with live trading algorithms that use the same portfolio.
- Consider adding indexes (for example, on the `date` column) when the table is expected to store millions of rows.
