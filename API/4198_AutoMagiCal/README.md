# AutoMagiCal Strategy

## Overview
The AutoMagiCal strategy is a faithful conversion of the MetaTrader script with the same name. The original tool produced a deterministic "magic number" that other expert advisors could share to tag their orders. Instead of placing trades, the script simply analysed the symbol name and displayed the resulting magic identifier on the MetaTrader chart. The StockSharp port replicates this behaviour inside the strategy lifecycle so that automated workflows can reuse the identifier in exactly the same way.

Unlike typical trading strategies, AutoMagiCal focuses solely on data preparation. When the strategy starts it inspects the assigned `Security`, extracts selected characters from the symbol, converts them to ASCII codes, and stitches the digits together into a long integer. The resulting number is stored in the `MagicNumber` property and written to the strategy log so that other strategies or manual operators can reference it.

## Magic number algorithm
1. Read the instrument identifier. The strategy first tries `Security.Id` and falls back to `Security.Code` if the identifier is unavailable. An error is reported when no security is attached.
2. Extract four characters using the same offsets as the MetaTrader script: positions 1 to 4 of the identifier (skip the very first character). Each character is converted to its ASCII decimal code before being appended to an intermediate string.
3. Convert the string of concatenated digits to a decimal number. Parsing errors are reported through `AddErrorLog`, mimicking the defensive checks that were necessary in the MQL code.
4. Reduce extremely large results using the same thresholds as the original script. Numbers above `999,999,999` are divided by 10; numbers above `9,999,999,999` after the first adjustment receive an additional division by 100. These guards prevent integer overflow when the digits create a very long number.
5. Round the adjusted value to the nearest integer and expose it through `MagicNumber`. The value is also printed in the log with the message `"Magic No. = X"`, reproducing the label that MetaTrader drew in the chart corner.

## Implementation notes
- The class inherits from `Strategy` and overrides `OnStarted` to run the calculation once the strategy begins. `OnReseted` clears the stored magic number so new runs always recompute the value from scratch.
- All comments in the source code are written in English, conforming to the repository guidelines.
- The algorithm is deliberately kept free of trading logic. The strategy neither registers orders nor subscribes to market data; it serves exclusively as a utility component that other strategies can query for a stable identifier.
- Log messages are used instead of chart labels. StockSharp exposes the log through the UI, files, and telemetry so the message remains easy to access even without graphical overlays.
- The `MagicNumber` property is nullable. It stays `null` until the first successful calculation, which allows consumers to detect configuration problems.

## Usage instructions
1. Attach the strategy to a portfolio and assign the desired security before calling `Start()`. The magic number calculation cannot proceed without an instrument identifier.
2. Start the strategy. `OnStarted` triggers immediately and runs the AutoMagiCal logic.
3. Inspect the logs to find the information entry `Magic No. = <value>`. The same value is available programmatically through the `MagicNumber` property.
4. Optionally stop the strategy once the value is retrieved. Because the strategy performs no trading actions, it can remain running harmlessly if you prefer to keep the magic number in memory.
5. If the log contains an error such as “Security is not assigned” or “Symbol is too short”, review the instrument identifier. Ensure that it contains at least five characters so the extraction step can mirror the original script.

## Differences from the MetaTrader version
- The MetaTrader script drew a label in the chart window. The StockSharp port replaces this user interface element with log messages, which are better aligned with cross-platform automation.
- AutoMagiCal for StockSharp never divides the logic across multiple source files. Both the calculation and the error handling reside in a single strategy class to comply with the repository structure.
- The conversion explicitly rounds the intermediate value using .NET’s `Math.Round` with `MidpointRounding.AwayFromZero`. MetaTrader’s `NormalizeDouble` used the same rounding rule, but the explicit statement clarifies the behaviour for StockSharp users.
- MetaTrader relied on implicit type conversions from double to int. The C# version stores the result in a nullable integer so the presence of a successful calculation is always explicit.

## Troubleshooting
- **No security assigned** – Attach the strategy to an instrument before starting. The algorithm needs the symbol string to produce deterministic digits.
- **Symbol shorter than five characters** – The original script expected at least five characters to build its magic number. If your trading symbols are shorter, consider padding them or modifying the algorithm to pick different offsets.
- **Unexpected magic number** – Remember that ASCII codes depend on the exact characters inside the identifier. Differences between broker symbol formats (e.g. `EURUSD` vs `EURUSDm`) will produce different results even for the same underlying instrument.

## Parameters
The strategy does not expose runtime parameters. All behaviour is fixed to match the MetaTrader implementation, ensuring that the generated magic number is consistent across both platforms.
