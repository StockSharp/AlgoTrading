# Forex Sky Strategy

## Overview
The **Forex Sky Strategy** is a direct port of the MetaTrader expert advisor `Forex_SKY.mq4`. It trades MACD momentum swings and strictly limits itself to a single position per trading day. The StockSharp implementation keeps the original MACD thresholds and the safety check that prevents more than one order per candle.

The strategy subscribes to the timeframe defined by `CandleType` (15-minute candles by default) and evaluates the classic MACD (12/26/9) on the close of each completed candle.

## Trading Logic
- **Long entry** – Place a market buy when:
  - The current MACD main line is above zero;
  - It also exceeds `+0.00009` to confirm momentum;
  - At least one of the previous three MACD readings was less than or equal to zero (capturing a bullish flip from negative territory).
- **Short entry** – Place a market sell when either of the following is true:
  - The MACD main line is below zero, drops below `-0.0004`, at least one of the last three readings was non-negative, and the value from four bars ago was at least `+0.001`.
  - **Or** the value from four bars ago was `≥ +0.003`, which immediately authorises a short trade just like in the original MetaTrader code.
- **Position management** – The algorithm never opens more than one order per candle (`Time0` guard) and never trades more than once per calendar day (`CheckTodaysOrders` guard). Protective exit orders are handled by the StockSharp `StartProtection` helper, so all stops and targets remain synchronized with the current volume.

There is no autonomous flatting logic beyond the protective orders—positions are expected to be closed by take-profit, stop-loss or manual intervention, mirroring the behaviour of the original expert advisor.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `FastPeriod` | 12 | Fast EMA length of the MACD indicator. |
| `SlowPeriod` | 26 | Slow EMA length of the MACD indicator. |
| `SignalPeriod` | 9 | Signal EMA length of the MACD indicator. |
| `TakeProfitPoints` | 100 | Distance to the take-profit order expressed in instrument points. Converted to price by multiplying with the security price step. |
| `StopLossPoints` | 3000 | Distance to the stop-loss order in instrument points. |
| `TradeVolume` | 0.1 | Base market order size (lots). |
| `CandleType` | 15-minute time frame | Timeframe that feeds the MACD calculations and trade decisions. |

### Instrument point calculation
`TakeProfitPoints` and `StopLossPoints` are specified exactly like the MetaTrader version—`Point` in MQL4 corresponds to `Security.PriceStep` in StockSharp. For a five-digit forex pair (`PriceStep = 0.00001`), the default settings translate to:
- Take-profit: `100 × 0.00001 = 0.001` price units.
- Stop-loss: `3000 × 0.00001 = 0.03` price units.

## Risk Management
`StartProtection` automatically installs the take-profit and stop-loss orders after an entry is filled. They are linked to the trade direction and use market orders when triggered, matching the MetaTrader behaviour. Set either parameter to `0` to disable the corresponding protective order.

## Migration Notes
- The MACD history buffer keeps the last four completed values in class fields so no indicator calls with shifted indices are required.
- Daily trade throttling and the single-trade-per-bar constraint replicate `CheckTodaysOrders()` and `Time0` from the original source.
- All comments were rewritten in English, and the logic relies on StockSharp high-level bindings (`Bind`) for indicator processing.

## Usage Tips
- Adjust `CandleType` to the chart period you want to emulate; the original script inherits the chart timeframe automatically.
- Because only one trade is allowed per day, pick markets with meaningful intraday swings or consider raising the MACD thresholds when using higher volatility instruments.
- Monitor the platform clock/time zone to ensure the day boundary matches your trading session, as the limit counter resets based on the candle open date.
