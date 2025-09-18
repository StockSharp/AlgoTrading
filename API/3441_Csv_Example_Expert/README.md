# Csv Example Expert Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Conversion of the MetaTrader 4 expert advisor `CsvExampleExpert.mq4`. The strategy keeps exactly one market position open in the
selected direction and closes it using symmetric take-profit and stop-loss offsets measured in MetaTrader points. After an exit
it immediately looks for the next opportunity to re-open a position in the same direction. Optional CSV logging reproduces the
original EA feature that stored the outcome of every trade.

## Details

- **Entry logic**: once the strategy is online and there are no active orders or open position it sends a market order in the
  direction specified by `TradeDirection`.
- **Exit logic**: prices are monitored through Level1 data (best bid/ask). Long positions are closed when the ask price reaches
  either the configured take-profit or stop-loss distance from the entry price. Short positions use the bid price and identical
  distances.
- **Position management**: only one position is allowed. As soon as the position is closed the next entry is queued, keeping a
  continuous exposure similar to the original expert advisor.
- **Data**: Level1 subscription is the only data feed required because the logic relies exclusively on bid/ask updates.
- **CSV logging**: when `WriteCloseData` is enabled the strategy rewrites the configured file at start, writes a header, and then
  appends one line per closed position (`direction, gain, close price, close time, symbol, volume`). Decimal values are saved by
  using the invariant culture to match the MT4 output style.
- **Default parameter values**:
  - `TradeVolume` = 0.1 lots
  - `TakePoints` = 300 points
  - `StopPoints` = 300 points
  - `TradeDirection` = Sell
  - `WriteCloseData` = false
  - `FileName` = `CSVexpert/CSVexample.csv`
- **Filters**:
  - Category: Trend following
  - Direction: Single fixed side (user-defined)
  - Indicators: None
  - Stops: Fixed take-profit and stop-loss
  - Complexity: Basic
  - Timeframe: Tick/Level1 updates
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium (permanent market exposure)

## Parameter mapping

| MT4 input                  | StockSharp parameter | Description |
|----------------------------|----------------------|-------------|
| `TradedLot`                | `TradeVolume`        | Lot size used for the market orders. |
| `take`                     | `TakePoints`         | Take-profit distance in MetaTrader points. |
| `stop`                     | `StopPoints`         | Stop-loss distance in MetaTrader points. |
| `OpType`                   | `TradeDirection`     | Direction of the maintained position (Buy or Sell). |
| `WriteCloseData`           | `WriteCloseData`     | Enables CSV logging for closed trades. |
| `FileName`                 | `FileName`           | File path for the CSV report, identical to the original EA default. |

## Implementation notes

- The strategy translates MetaTrader points into absolute price distance by multiplying by the instrument price step. If the
  board does not expose a price step the fallback value `0.0001` is used to keep the logic operational.
- The realised PnL delta produced by StockSharp is used when logging trades so that commissions and slippage are captured in the
  report.
- The CSV writer recreates directories automatically for relative paths, allowing the default `CSVexpert` folder to be generated
  on first run.
