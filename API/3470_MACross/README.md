# MACross Strategy

The strategy replicates the behaviour of the original `MQL/34176/MACross.mq4` expert advisor using the StockSharp high-level API. It trades a single instrument on a moving-average crossover and keeps all risk controls expressed in pips and account equity.

## Trading logic

1. Two simple moving averages (SMA) are built on the configured candle type:
   - `FastPeriod` reacts quickly to price changes.
   - `SlowPeriod` smooths the longer-term trend.
2. At the close of each finished candle the fast and slow averages are compared:
   - A bullish crossover (fast crossing above slow) opens a long position. Any active short is flattened first.
   - A bearish crossover (fast crossing below slow) opens a short position after closing an existing long.
3. Every entry uses a fixed market volume derived from `LotSize` and aligned with the instrument limits (`VolumeStep`, `MinVolume`, `MaxVolume`).
4. After the position is opened the strategy tracks two risk targets measured in pips. The pip size is automatically inferred from `Security.Decimals` (or `PriceStep` as a fallback):
   - `TakeProfitPips` defines the distance to the profit target. Hitting it issues a market exit in the current direction.
   - `StopLossPips` defines the protective stop distance. Breaching it closes the position immediately.
5. Trading can be paused by the `MinEquity` guard. When the current portfolio value is below the threshold the strategy keeps managing the active position but does not allow new entries.

All calculations work on finished candles only, fully matching the original expert advisor that waited for a new bar before evaluating the moving averages.

## Visualisation

When a chart pane is available the strategy plots:

- Input candles from the subscribed series.
- The fast and slow SMAs.
- Own trades to highlight entries and exits triggered by the crossover rules.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `8` | Length of the fast SMA that generates crossover signals. |
| `SlowPeriod` | `int` | `20` | Length of the slow SMA used as the reference trend line. |
| `TakeProfitPips` | `decimal` | `20` | Profit target distance expressed in pips. The pip size is inferred from the instrument decimals. |
| `StopLossPips` | `decimal` | `20` | Protective stop distance in pips. Uses the same pip size calculation as the profit target. |
| `LotSize` | `decimal` | `1` | Base order volume. The strategy rounds it to the nearest allowed size before sending market orders. |
| `MinEquity` | `decimal` | `100` | Minimum account equity. New trades are blocked while the portfolio value is below this level. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Candle series used for SMA calculations and signal evaluation. |

## Differences vs. MQL version

- The original MQL expert passed stop-loss and take-profit prices to `OrderSend` as zero. The StockSharp port emulates the same behaviour with manual exits that monitor the close price of each finished candle.
- Equity validation (`cekMinEquity`) now reads `Portfolio.CurrentValue` and `Portfolio.BeginValue` instead of `AccountEquity()` but preserves the threshold logic.
- Pip size detection mirrors the `GetPipPoint` helper: 2- or 3-digit quotes use 0.01, 4- or 5-digit quotes use 0.0001, otherwise `PriceStep` is taken.

The resulting strategy can be optimised through all exposed parameters and combines seamlessly with StockSharp charting and risk management infrastructure.
