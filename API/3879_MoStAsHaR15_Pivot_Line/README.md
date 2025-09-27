# MoStAsHaR15 Pivot Line Strategy

## Overview
This strategy reproduces the "MoStAsHaR15 FoReX - Pivot Line" MetaTrader 4 expert using StockSharp's high level strategy API. It keeps the original daily floor-pivot map combined with momentum filters from ADX, EMA spreads and the MACD histogram (OsMA). Intraday logic operates on an hourly candle stream while a second subscription consumes the previous completed daily candle to rebuild the pivot ladder before each decision.

## Trading Logic
- **Pivot calculation** – yesterday's high, low and close from the daily series generate the classic pivot (P), three resistance levels (R1–R3), three support levels (S1–S3) and six midpoints (M0–M5). The current candle close is checked against this ladder to detect the surrounding range. The unusual mapping from the original EA that links the region between M5 and R3 back to the S3/M0 segment is preserved.
- **Distance filter** – trades only trigger when the distance to the take-profit boundary that caps the current range is greater than `MinimumDistancePips` (14 pips by default), mirroring the original `dif1`/`dif2` checks.
- **Long entries** require all of the following filters:
  - ADX main line is above `AdxThreshold` (20) and the +DI component is both rising and stronger than −DI.
  - The close-based EMA is at least `EmaSpreadPips` (5 pips) above the open-based EMA, and the previous candle already had the same bullish ordering.
  - The MACD histogram increased compared to the previous candle (OsMA rising).
- **Short entries** mirror the long branch with −DI strength, bearish EMA spread and a falling MACD histogram.
- Only one net position is allowed at any time. Orders are sent with market execution using `BuyMarket()` and `SellMarket()`.

## Position Management
- **Stop-loss** – optional, located `StopLossPips` below/above the entry price. Set to `0` to disable as in the original EA.
- **Take-profit** – fixed at the pivot boundary (support or resistance) that delimits the current price range when the trade is opened.
- **Trailing stop** – once price advances more than `TrailingStopPips + TrailingStepPips` beyond the entry, the stop is trailed to maintain a distance of `TrailingStopPips`. The step value must remain positive whenever trailing is enabled.
- If the stop-loss, trailing stop or take-profit is touched inside a candle, the position is closed on that bar's evaluation.

## Strategy Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `HourlyCandleType` | Intraday candle series feeding the execution logic. | 1 hour |
| `DailyCandleType` | Daily candle stream used to compute pivot levels. | 1 day |
| `StopLossPips` | Initial stop-loss distance in pips. `0` disables it. | 20 |
| `TrailingStopPips` | Trailing stop distance in pips. | 10 |
| `TrailingStepPips` | Minimum move (in pips) before the trailing stop updates. Must be > 0 when trailing is enabled. | 5 |
| `MinimumDistancePips` | Minimum pip distance to the nearby pivot boundary before entering a trade. | 14 |
| `EmaSpreadPips` | Required spread between the close EMA and open EMA. | 5 |
| `AdxThreshold` | Minimum ADX reading that activates the signal. | 20 |
| `AdxPeriod` | ADX indicator period. | 14 |
| `EmaClosePeriod` | EMA length applied to candle closes. | 5 |
| `EmaOpenPeriod` | EMA length applied to candle opens. | 8 |
| `MacdFastPeriod` | Fast EMA period for MACD (OsMA numerator). | 12 |
| `MacdSlowPeriod` | Slow EMA period for MACD. | 26 |
| `MacdSignalPeriod` | Signal EMA period for MACD. | 9 |

## Conversion Notes
- Indicator values are evaluated only on finished candles, and no rolling collections are stored – state is managed through scalar fields per repository guidelines.
- Pips are derived from the security's `PriceStep` and decimal precision. Symbols quoted with 3 or 5 decimal places use the "mini pip" convention just like MetaTrader.
- The take-profit mapping for the M5→R3 region intentionally falls back to the S3/M0 pair to stay faithful to the source code.
- All comments inside the strategy remain in English as required by the project instructions.

## Usage Tips
- Adjust the candle types to match the trading session of your instrument, especially for markets with non-standard daily rollovers.
- Because the logic evaluates stops and targets on closed candles, additional slippage compared to tick-level MetaTrader execution may occur in fast markets.
- Consider tuning `MinimumDistancePips` and `EmaSpreadPips` when applying the strategy to assets with different volatility regimes.
