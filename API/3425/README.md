# ABH_BH_MFI Strategy

## Overview
The **ABH_BH_MFI Strategy** is a StockSharp high-level port of the MetaTrader expert advisor "Expert_ABH_BH_MFI". The algorithm combines bullish and bearish Harami candlestick patterns with confirmation from the Money Flow Index (MFI). Long trades are triggered when a bullish Harami forms inside a falling market while the MFI remains depressed. Short trades require a bearish Harami inside a rising market and an elevated MFI. The original MQL implementation relied on MetaTrader's signal infrastructure; this conversion keeps the decision logic but expresses it with StockSharp's candle subscriptions, indicator binding, and position management helpers.

## Trading Logic
### 1. Harami pattern detection
- The strategy stores the two most recent completed candles.
- A **bullish Harami** requires:
  - Two candles ago was a long black (bearish) candle whose body is larger than the average body length.
  - The most recent candle is bullish and its open/close are engulfed by the body of the previous bearish candle.
  - The midpoint of the older candle lies below the simple moving average of closes, signalling a prevailing downtrend.
- A **bearish Harami** mirrors these requirements with inverted colours and the midpoint above the moving average to confirm an uptrend.

### 2. Money Flow Index confirmation
- The MFI uses the configurable `MfiPeriod` (default **37**) to replicate the original oscillator settings.
- Long entries demand that the latest completed MFI value stays below `BullishThreshold` (default **40**) to ensure capital inflow exhaustion.
- Short entries require the MFI to remain above `BearishThreshold` (default **60**) to show buying pressure exhaustion.

### 3. Exit rules through MFI crossovers
- Active long positions are closed when the MFI crosses above either `ExitLowerLevel` (default **30**) or `ExitUpperLevel` (default **70**), matching the MetaTrader conditions `MFI(1) > level && MFI(2) < level`.
- Active short positions are closed when the MFI crosses down from the overbought zone or spikes below the oversold level, mirroring the original short exit clauses.

### 4. Risk management
- The strategy optionally applies `StartProtection` with stop-loss and take-profit offsets expressed in price steps. Setting the corresponding parameter to zero disables the protective distance, reproducing the MetaTrader defaults.
- Position sizing uses the base `Volume` property; reversing positions automatically adds enough contracts to flatten and reopen in the new direction, just like the source expert.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 1-hour time frame | Primary candle series analysed for patterns and MFI. |
| `MfiPeriod` | 37 | Lookback for the Money Flow Index indicator. |
| `BodyAveragePeriod` | 11 | Length of the simple moving averages that measure body size and closing trend. |
| `BullishThreshold` | 40 | Maximum MFI value allowed before opening long trades. |
| `BearishThreshold` | 60 | Minimum MFI value required before opening short trades. |
| `ExitLowerLevel` | 30 | Lower MFI crossover level for position exits. |
| `ExitUpperLevel` | 70 | Upper MFI crossover level for position exits. |
| `StopLossPoints` | 0 | Optional stop-loss distance in price steps (0 disables). |
| `TakeProfitPoints` | 0 | Optional take-profit distance in price steps (0 disables). |

## Implementation Notes
- Candle data are received via `SubscribeCandles(CandleType)` and processed only when the candle state is `Finished`, ensuring alignment with closed-bar logic of the MQL expert.
- The MFI indicator is bound directly with `.Bind(_mfi, ProcessCandle)` so that the handler receives ready-to-use decimal values without calling `GetValue`.
- Two auxiliary simple moving averages replicate the `AvgBody` and `CloseAvg` helper functions from the MetaTrader code. Their results are cached to avoid historical indicator queries.
- Exit and entry decisions call `IsFormedAndOnlineAndAllowTrading()` before sending orders, staying consistent with StockSharp's recommended trading safety checks.

## Differences from the MetaTrader Expert
- Money management is simplified to the base strategy volume. The original "fixed lot" module translated to StockSharp's position sizing helper, which covers the same functionality without separate classes.
- The MetaTrader trailing stop component (`TrailingNone`) had no logic; the StockSharp version therefore omits any trailing actions but keeps optional fixed risk targets.
- Logging is minimal by default; you may extend it with `LogInfo` calls if you need verbose trade diagnostics.

## Usage Tips
1. Configure the desired security and assign the `CandleType` before starting the strategy.
2. Optionally adjust the MFI and exit thresholds to suit different volatility regimes.
3. Provide non-zero `StopLossPoints`/`TakeProfitPoints` when the broker requires explicit protective orders; otherwise leave them at zero to trade without hard targets.
4. Monitor the chart panes created by the strategy to visualise candles, the MFI indicator, and executed trades.
