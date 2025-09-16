# AO Lightning Strategy

## Overview

AO Lightning reproduces the MT5 expert advisor "AO_Lightning" using the high-level StockSharp API. The system monitors the slope of the Awesome Oscillator (AO) built from median prices. When the oscillator decreases the strategy accumulates long exposure, and when the oscillator increases it builds a short position. Positions are pyramided up to a configurable cap while opposite positions are closed before flipping direction.

## Trading Logic

1. Subscribe to the selected candle series and calculate the Awesome Oscillator with short period 5 and long period 34 (the defaults taken from the original MQL code).
2. Wait for finished candles only; the strategy ignores interim updates to avoid double-counting.
3. On the first finished candle the AO value is stored as a reference.
4. When the current AO value is **lower** than the previous value:
   - If an open short position exists, send a single market buy order sized to close the entire short and immediately add one long layer.
   - If no short is present and the long exposure is below the limit, buy one additional layer.
5. When the current AO value is **higher** than the previous value:
   - If an open long position exists, send a single market sell order that closes the long exposure and simultaneously opens one short layer.
   - If no long is present and the short exposure is below the limit, sell one additional layer.
6. AO values equal to the previous reading leave the position unchanged.
7. Built-in `StartProtection()` is enabled once at start-up so Designer users can attach stops or other risk modules if desired.

The logic mirrors the original expert advisor: AO slope defines trade direction, opposite trades are flattened before a new entry, and incremental orders keep stacking until the cap is reached.

## Position Management

- **Trade volume** defines the size of each additional layer and corresponds to the MT5 parameter `LotFixed`.
- **Max positions** matches the MT5 `Orders` input. It restricts how many layers may accumulate on either side.
- **Pyramiding** is linear: each valid signal adds exactly one lot-sized layer provided the cap has not been reached.
- **Flattening** sends combined orders (close + new direction) to avoid intermediate flat states when flipping from short to long or vice versa.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Order size for every new layer. | 1 |
| `MaxPositions` | Maximum number of long or short layers that may be active simultaneously. | 10 |
| `AoShortPeriod` | Fast SMA length used by the Awesome Oscillator (median price SMA). | 5 |
| `AoLongPeriod` | Slow SMA length for the Awesome Oscillator. | 34 |
| `CandleType` | Candle data source processed by the strategy. | 5-minute time frame |

## Notes

- The original MT5 expert names the inputs `Period_sma_slow` and `Period_sma_fast` but swaps the values (5 and 34). The StockSharp port keeps the functional mapping by exposing intuitive `AoShortPeriod`/`AoLongPeriod` parameters.
- No Python version is provided yet, matching the task request.
- Tests are not included; run any necessary validations via Designer or your own backtesting harness before deploying to production.
