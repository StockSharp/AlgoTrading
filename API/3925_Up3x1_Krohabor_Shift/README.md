# Up3x1 Krohabor Shift Strategy

## Overview
The **up3x1 Krohabor D** strategy is a conversion of the MetaTrader 4 expert advisor `up3x1_Krohabor_D.mq4`. It keeps the original idea of aligning three displaced simple moving averages (SMA) to detect trend continuation breakouts on the active timeframe. The C# implementation uses the high-level StockSharp API with candle subscriptions and indicator bindings, while adapting risk and position management to the .NET environment.

## Trading Logic
- Three SMAs are calculated on the instrument's closing prices:
  - Fast SMA (default 24 bars)
  - Medium SMA (default 60 bars)
  - Slow SMA (default 120 bars)
- Every moving average is shifted forward by a configurable number of completed candles (default 6). The strategy compares the current shifted value and the value from the previous candle for each average.
- **Long entry** requirements:
  - Both the current and previous slow SMA values stay below the current and previous fast/medium SMA values, indicating bullish separation.
  - The medium SMA is falling relative to the fast SMA (previous medium above previous fast, current medium below current fast).
- **Short entry** mirrors the long logic with all comparisons reversed.
- Only one position can be open at a time. When no position is active the strategy waits for a new entry signal; otherwise it manages exits.

## Exit Rules and Protection
- Initial protective orders are simulated by monitoring candle highs and lows:
  - Stop-loss distance is expressed in price steps (default 110 points) and applied once a position is opened.
  - Take-profit distance uses the same representation (default 5 points).
- A trailing stop (default 10 points) activates once unrealised profit exceeds the configured threshold. The stop follows the market in favor of the open position while never retreating.
- Moving-average reversal exits close the trade when the fast SMA crosses back through the medium and slow averages, imitating the original EA's closing logic.
- Dynamic volume reduction after consecutive losses replicates the MT4 script behaviour: the trade size decreases proportionally to the number of losing trades while respecting a minimum volume floor.

## Parameters
| Name | Description |
|------|-------------|
| `FastPeriod` | Period of the fast SMA. |
| `MediumPeriod` | Period of the medium SMA. |
| `SlowPeriod` | Period of the slow SMA. |
| `MaShift` | Number of completed candles used to shift all moving averages forward. |
| `Volume` | Base order volume for new entries. |
| `MinVolume` | Minimum allowed volume after loss-based adjustments. |
| `LossReductionFactor` | Divisor applied when shrinking volume after consecutive losing trades. |
| `StopLossPoints` | Stop-loss distance measured in price steps. |
| `TakeProfitPoints` | Take-profit distance measured in price steps. |
| `TrailingPoints` | Trailing-stop distance and activation threshold in price steps. |
| `CandleType` | Candle data type (timeframe) used for analysis. |

## Notes
- The strategy uses `SubscribeCandles` together with `Bind` to stream indicator outputs, avoiding manual indicator value retrieval.
- Stop-loss, take-profit, and trailing behaviour is implemented inside the strategy loop to stay broker-independent. In live trading environments you can replace these blocks with actual protective orders if required.
- All comments within the source code are written in English to comply with project guidelines.
- No automated tests are provided; use backtesting inside StockSharp to validate parameter sets for your instruments.
