# Starter V6 Mod Strategy (StockSharp Conversion)

## Overview

The **Starter V6 Mod** strategy is a StockSharp high-level API conversion of the MetaTrader 5 Expert Advisor `Starter_v6mod`. The original system combines a Laguerre RSI oscillator, dual exponential moving averages, a commodity channel index filter, and a grid-style position management module. This port preserves the multi-layered confirmation logic while adapting position handling, money management, and protective actions to the StockSharp environment.

## Trading Logic

### Indicators

* **Laguerre RSI proxy** – modeled via a normalized 14-period RSI to emulate the 0-1 scale used by the original Laguerre oscillator. The level pair `LevelDown` / `LevelUp` (default 0.15 / 0.85) defines oversold and overbought zones.
* **Slow EMA (120)** and **Fast EMA (40)** – both calculated on the median candle price. Their relative displacement acts as a trend direction filter. The `AngleThreshold` parameter converts the EMA spread into a tick distance that gates trading directions.
* **Commodity Channel Index (14)** – confirms momentum direction by requiring negative values for long entries and positive values for short entries.

### Entry Conditions

1. Determine the trend bias from the EMA spread:
   * If the slow EMA minus the fast EMA is less than `-AngleThreshold` ticks, only long positions may be initiated.
   * If the spread is greater than `AngleThreshold`, only shorts may be initiated.
   * Otherwise, the market is considered flat and no new positions are opened.
2. When the trend bias allows a direction, check the oscillator and momentum filters:
   * Long setup – Laguerre proxy below `LevelDown`, slow EMA < previous slow EMA, fast EMA < previous fast EMA, and CCI < 0.
   * Short setup – Laguerre proxy above `LevelUp`, slow EMA > previous slow EMA, fast EMA > previous fast EMA, and CCI > 0.
3. Grid spacing – when stacking positions in the same direction, the current price must be at least `GridStepPips` below the lowest long entry or above the highest short entry. This replicates the averaging logic of the original EA.
4. Position count – the total number of simultaneous grid entries cannot exceed `MaxOpenTrades`.

### Exit Conditions

* **Laguerre exits** – longs close when the oscillator crosses above `LevelUp`; shorts close when it falls below `LevelDown`.
* **Stop-loss / Take-profit** – expressed in pips, converted to instrument price increments. The conversion tracks the original adjustment for symbols with 3/5 decimal pricing.
* **Trailing stop** – activates after price advances by `(TrailingStopPips + TrailingStepPips)` and then follows price with an offset of `TrailingStopPips`.
* **Friday protections** – no new trades are allowed after 18:00 (terminal time) and all open positions are liquidated after 20:00.

### Money Management

* **Volume sizing** – either fixed (`UseManualVolume = true`) or risk-based. In risk mode, volume equals `(equity * RiskPercent) / (StopLoss distance in price units)`.
* **Equity cutoff** – trading stops when the current equity falls below `EquityCutoff`.
* **Daily loss limit** – if the strategy records `MaxLossesPerDay` losing exits on the current date, no further positions are opened.
* **Loss recovery** – after each losing exit, the next position size is divided by `DecreaseFactor^lossesToday`, mirroring the original position scaling logic.

## Implementation Notes

* The conversion uses the StockSharp high-level `SubscribeCandles().Bind(...)` pipeline to stream finished candles and indicator values into the decision logic.
* StockSharp does not ship a native Laguerre RSI, so a normalized RSI is used as a proxy. The thresholds match the 0-1 Laguerre range.
* The EMA angle filter is reproduced by measuring the spread between the slow and fast EMA values in ticks, providing a directional gate similar to the original `emaangle` custom indicator.
* Manual stop and trailing management are performed within the candle processing routine to maintain parity with the MQL trailing modifications.
* Grid bookkeeping tracks average entry price, lowest/highest fill price, and trailing levels to emulate the MQL multi-position workflow while working within StockSharp’s aggregated position model.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `UseManualVolume` | `false` | Toggle between fixed and risk-based position sizing. |
| `ManualVolume` | `1` | Volume used when manual sizing is enabled or risk sizing cannot be computed. |
| `RiskPercent` | `5` | Percentage of equity risked per trade when automatic sizing is active. |
| `StopLossPips` | `35` | Stop-loss distance in pips. |
| `TakeProfitPips` | `10` | Take-profit distance in pips. |
| `TrailingStopPips` | `0` | Trailing stop distance in pips (0 disables trailing). |
| `TrailingStepPips` | `5` | Minimum advance before the trailing stop begins to follow price. |
| `DecreaseFactor` | `1.6` | Factor applied to reduce size after each loss. |
| `MaxLossesPerDay` | `3` | Maximum losing exits permitted per calendar day. |
| `EquityCutoff` | `800` | Equity threshold that halts new trades. |
| `MaxOpenTrades` | `10` | Maximum number of simultaneous grid entries. |
| `GridStepPips` | `30` | Minimum spacing between stacked entries in the same direction. |
| `LongEmaPeriod` | `120` | Period of the slow EMA filter. |
| `ShortEmaPeriod` | `40` | Period of the fast EMA filter. |
| `CciPeriod` | `14` | Commodity Channel Index period. |
| `AngleThreshold` | `3` | EMA spread threshold expressed in ticks. |
| `LevelUp` | `0.85` | Upper Laguerre level. |
| `LevelDown` | `0.15` | Lower Laguerre level. |
| `CandleType` | `15m` | Candle timeframe used for calculations. |

## Usage Tips

1. Configure the `CandleType` parameter to match the timeframe used in the original MT5 setup (the EA is often deployed on 15-minute charts).
2. Align risk settings with account specifications. When using risk-based sizing, ensure `StopLossPips` reflects the instrument’s volatility because it directly affects calculated volume.
3. Review exchange trading hours. The built-in Friday protection assumes the server clock aligns with the desired session close.
4. Enable chart drawing (via `CreateChartArea`) to visualize EMA, RSI proxy, CCI, and executed trades for debugging or optimization.
5. When porting parameter sets from MT5 backtests, remember that the RSI proxy approximates the Laguerre oscillator; minor threshold tuning may be needed to match original signal timing.

## Files

* `CS/StarterV6ModStrategy.cs` – StockSharp strategy implementation.
* `README.md` – English documentation (this file).
* `README_cn.md` – Simplified Chinese documentation.
* `README_ru.md` – Russian documentation.

