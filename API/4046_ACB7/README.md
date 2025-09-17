# ACB7 Matrix Benchmark

## Overview
ACB7 Matrix Benchmark is a direct port of the MetaTrader 4 script `ACB7`. The original script demonstrates a tiny matrix library by multiplying a 2×3 matrix by a 3×2 matrix, storing the result in a 2×2 matrix, and repeating the operation 1,000 times to profile execution time. The StockSharp version replicates this computation inside a `Strategy` so that the benchmark can be launched from the platform and observed through the strategy log.

## Strategy logic
### Matrix definitions
- **Matrix 1** – ID 1, 2 rows × 3 columns, values `[1, 2, 3; 4, 5, 6]`.
- **Matrix 2** – ID 2, 3 rows × 2 columns, values `[7, 8; 9, 10; 11, 12]`.
- **Matrix 3** – ID 3, dynamically resized to 2 × 2 before storing the product.

### Processing steps
1. On reset the matrices are reconstructed from the original arrays stored in the script.
2. When the strategy starts it repeats the multiplication routine for the configured number of runs. Each run calls `ProcessMatrices`, the C# translation of `afi.MatrixProcessing`, which validates the dimensions, resizes the destination buffer, and calculates the product using triple nested loops.
3. After the final run the elapsed time is captured with `Stopwatch`. If the sizes are compatible, the matrices are printed through the strategy log in the same order as the original `Alert` calls: result matrix, second operand, first operand. A blank line is appended to mirror the trailing alert emitted by the MQL code.
4. If the dimensions ever become incompatible the routine exits early and records a failure message instead of logging matrix contents.

## Parameters
- **Runs** – number of times the multiplication routine is executed after the strategy starts. Defaults to `1000` to match the MQL script; can be optimized for stress testing.
- **Log Matrices** – toggles whether the matrices are written to the strategy log after a successful benchmark run.

## Additional notes
- The strategy does not subscribe to market data or place any orders; it is intended as a computational demo within the StockSharp environment.
- The helper class `MatrixBuffer` keeps the same row/column addressing logic as the original `afd.GetCell` and `afr.SetCell` functions, which makes the port straightforward to compare with the MQL implementation.
- After logging the output the strategy calls `Stop()` to terminate itself, emulating the script's one-shot behavior.
