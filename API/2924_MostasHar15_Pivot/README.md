# MostasHaR15 Pivot Strategy

## Overview
The strategy replicates the behaviour of the original **MostasHaR15 Pivot** MQL5 Expert Advisor using StockSharp's high level API. It combines classic daily floor-pivot calculations with momentum filters from ADX, EMA differentials and the MACD histogram (OsMA). The strategy operates on an intraday candle stream (1 hour by default) and consumes the previous completed daily candle to rebuild the pivot map on every bar.

## Trading Logic
- **Pivot grid** – the previous daily high, low and close are used to calculate the main pivot (P), three resistance levels (R1–R3), three support levels (S1–S3) and six midpoints (M0–M5). The current candle close is compared with this ladder to identify the surrounding support and resistance segment. A special case inherited from the EA maps prices between M5 and R3 back to the S3/M0 range.
- **Distance filter** – trades are only allowed when the distance to the nearest take-profit level is larger than `MinimumDistancePips` (14 pips by default), which matches the original `dif1`/`dif2` filters.
- **Long entries** require all of the following:
  - ADX main line exceeds `AdxThreshold` (20) and +DI is both rising and above –DI.
  - The 5-period EMA on candle closes is at least `EmaSlopePips` (5 pips) above the 8-period EMA on candle opens, and the previous bar showed the same bullish EMA ordering.
  - MACD histogram (OsMA) increased compared to the previous bar.
- **Short entries** mirror the long conditions with −DI strength, bearish EMA spread and a falling MACD histogram.
- Only one net position is allowed. Orders are placed with market execution via `BuyMarket()`/`SellMarket()`.

## Position Management
- **Stop-loss** – optional, located `StopLossPips` below/above the entry price. Setting the parameter to `0` disables the initial stop, as in the EA.
- **Take-profit** – fixed at the nearest pivot boundary that surrounds the current price when the position is opened.
- **Trailing stop** – replicates the original trailing logic. Once price advances more than `TrailingStopPips + TrailingStepPips` from entry, the stop is moved to maintain a trailing distance of `TrailingStopPips`. Trailing can be disabled by setting `TrailingStopPips` to `0`.
- If either stop-loss, trailing stop or take-profit is hit during a candle, the position is flattened at the close of that candle.

## Strategy Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Intraday candle series used for trading. | 1 hour time frame |
| `DailyCandleType` | Daily candle series for pivot calculations. | 1 day time frame |
| `StopLossPips` | Stop-loss distance in pips. Set `0` to disable. | 20 |
| `TrailingStopPips` | Trailing stop distance in pips. | 5 |
| `TrailingStepPips` | Minimum favourable move before the trail updates. Must be >0 if trailing is enabled. | 5 |
| `MinimumDistancePips` | Minimum pip distance to the nearest pivot boundary before entering a trade. | 14 |
| `EmaSlopePips` | Required separation between the close EMA and the open EMA. | 5 |
| `AdxThreshold` | Minimum ADX reading for both long and short trades. | 20 |
| `AdxPeriod` | ADX indicator length. | 14 |
| `EmaClosePeriod` | EMA period applied to candle closes. | 5 |
| `EmaOpenPeriod` | EMA period applied to candle opens. | 8 |
| `MacdFastPeriod` | Fast EMA period inside the MACD histogram. | 12 |
| `MacdSlowPeriod` | Slow EMA period inside the MACD histogram. | 26 |
| `MacdSignalPeriod` | Signal EMA period inside the MACD histogram. | 9 |

## Conversion Notes
- The strategy keeps the unusual behaviour of the EA where the price range between mid-level M5 and resistance R3 maps back to the S3/M0 support/resistance pair.
- All indicator values are processed on finished candles only. No historical collections are stored; all state is held in scalar fields as recommended by the repository guidelines.
- Comments in the strategy remain in English per repository instructions.

## Usage Tips
- Adjust `CandleType` and `DailyCandleType` when applying the strategy to markets with different trading sessions.
- Because stop-loss and trailing logic are evaluated on closed candles, additional slippage can appear in fast markets compared to tick-level execution in the original EA.
