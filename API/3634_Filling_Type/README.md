# Filling Type Strategy

The original MQL5 expert advisor "Filling Type" simply detects which order filling policy is available for the active symbol by querying `SymbolInfoInteger(SYMBOL_FILLING_MODE)` and printing a descriptive message for Fill or Kill (FOK), Immediate or Cancel (IOC) or Return execution.

This StockSharp port preserves that diagnostic purpose while adapting it to the metadata exposed by `ExchangeBoard` objects:

1. When the strategy starts it validates that `Strategy.Security` is assigned and that the security exposes an `ExchangeBoard` reference. If the board is missing the strategy logs a warning similar to the original EA leaving the comment panel blank.
2. The strategy first attempts to read well-known board properties such as `OrderExecutionTypes`, `ExecutionType` or other execution-related members via reflection. Different StockSharp connectors populate different property names, therefore the strategy iterates through a curated list of candidates and falls back to any property containing the words "Execution" or "Filling".
3. Every detected property is logged line by line. Enumerable values are printed with their index so that the resulting log clearly states whether FOK/IOC/RETURN (or any other broker-specific execution code) is available.
4. If the board does not expose any dedicated execution property the strategy inspects `ExchangeBoard.ExtensionInfo` for keys that contain `fill` or `exec`, which is how some connectors surface the supported execution modes.
5. As a final fallback a summary of generic board capabilities (`IsSupportMarketOrders`, `IsSupportStopOrders`, `IsSupportStopLimitOrders`, `IsSupportOddLots`, `IsSupportMargin`) is printed to help the user infer the behaviour manually.
6. Optional diagnostics can be printed by enabling the `LogBoardDiagnostics` parameter. This section includes basic board metadata (name, exchange, country, time zone) plus settlement and delivery settings so that operators have full context while adjusting their order requests.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `LogBoardDiagnostics` | `true` | When enabled, prints extended board information after the filling policy detection, including exchange identity, settlement mode, delivery board and working schedule. This is helpful when confirming that the right instrument is attached before placing live orders. |

## Usage Notes

- The strategy is diagnostic only; it does not place any trades. Attach it to the security you plan to trade and review the log output right after the strategy starts.
- Fill policies in StockSharp are driven by the exchange adapter, therefore connectors may expose the information under different property names. The reflection-based discovery keeps the implementation flexible without hard-coding a single broker contract.
- If neither board properties nor the extension dictionary contain execution hints the fallback log provides enough detail to manually check the broker documentation (e.g., some exchanges allow partial fills only for limit orders).
- Unlike the original MQL expert, there is no `Comment()` output on the chart panel. StockSharp strategies typically rely on `LogInfo` messages which are visible in the strategy log UI as well as in saved log files.

## Differences vs. MQL Version

- The MQL5 script evaluated the filling mode on every tick. The StockSharp version performs the detection once during `OnStarted` because the board metadata is static; repeating the inspection per candle would only spam the log.
- Instead of a switch statement against MetaTrader-specific flags, the StockSharp implementation reads connector metadata using reflection. This keeps the code resilient to future SDK updates and makes the strategy usable across multiple exchanges.
- Because StockSharp strategies do not have access to the MetaTrader chart comment window, all messages are routed through `LogInfo`/`LogWarning` rather than `Comment()`/`Print()`.

## Running the Strategy

1. Configure a connector and select the target security in the strategy settings.
2. Start the strategy. The log will show which property was used to determine the supported filling modes and list every detected value.
3. Optionally toggle `LogBoardDiagnostics` to gather additional board information for troubleshooting broker-specific order requirements.

This detailed log replicates the educational purpose of the original EA while fitting naturally into the StockSharp environment.
