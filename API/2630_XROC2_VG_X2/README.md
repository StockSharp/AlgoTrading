# XROC2 VG X2 Strategy

## Overview
The XROC2 VG X2 strategy is a multi-timeframe system that combines two smoothed rate-of-change streams. The higher timeframe acts as a directional filter while the lower timeframe produces concrete entry and exit signals. The original MetaTrader 5 expert advisor relied on the custom XROC2_VG indicator with flexible smoothing options and a money management module. The StockSharp port keeps the signal logic intact and exposes the key parameters as strategy inputs.

The strategy subscribes to two candle series:
- **Higher timeframe** (default 6 hours) – establishes the prevailing trend direction.
- **Lower timeframe** (default 30 minutes) – generates entries and exits by monitoring how the two smoothed ROC lines cross.

Both streams share the same rate-of-change calculation mode but use individual smoothing settings. By default the strategy applies Jurik moving averages, mimicking the MQL version. Advanced smoothing types that are not directly supported by StockSharp (JurX, ParMA, T3, VIDYA, AMA with phase control) fall back to the closest available moving average implementation.

## Trading Logic
1. **Trend detection (higher timeframe)**
   - Compute two smoothed ROC values using the configured periods and smoothing methods.
   - Evaluate the line pair on the bar defined by `HigherSignalBar`. If the fast line is above the slow line the trend is bullish, otherwise bearish. A neutral reading keeps the current trend at zero and disables trading.
2. **Signal generation (lower timeframe)**
   - Compute the same pair of smoothed ROC values on the lower timeframe.
   - Look at the most recent finished bar (shift `LowerSignalBar`) and the bar before it. The combination of these two bars determines whether a cross just happened.
   - A long setup appears when the higher timeframe is bullish, the fast line crossed below the slow line (downward cross), and longs are enabled.
   - A short setup appears when the higher timeframe is bearish, the fast line crossed above the slow line (upward cross), and shorts are enabled.
3. **Position management**
   - Close long positions when the lower timeframe cross indicates bearishness (`CloseBuyOnLower`) or when the higher timeframe trend flips to bearish (`CloseBuyOnTrendFlip`).
   - Close short positions when the lower timeframe cross becomes bullish (`CloseSellOnLower`) or when the higher timeframe trend flips to bullish (`CloseSellOnTrendFlip`).
   - New trades are opened only when no position is active. Order size is controlled by the strategy `Volume` property.

## Parameters
- `HigherCandleType` – candle type for the trend filter (default 6-hour time frame).
- `LowerCandleType` – candle type for signal generation (default 30-minute time frame).
- `HigherSignalBar` – how many closed bars to shift when reading higher timeframe values (default 1).
- `LowerSignalBar` – how many closed bars to shift when reading lower timeframe values (default 1).
- `HigherRocMode` / `LowerRocMode` – rate-of-change calculation variant (`Momentum`, `RateOfChange`, `RateOfChangePercent`, `RateOfChangeRatio`, `RateOfChangeRatioPercent`).
- `HigherFastPeriod`, `HigherFastMethod`, `HigherFastLength`, `HigherFastPhase` – fast ROC settings for the higher timeframe.
- `HigherSlowPeriod`, `HigherSlowMethod`, `HigherSlowLength`, `HigherSlowPhase` – slow ROC settings for the higher timeframe.
- `LowerFastPeriod`, `LowerFastMethod`, `LowerFastLength`, `LowerFastPhase` – fast ROC settings for the lower timeframe.
- `LowerSlowPeriod`, `LowerSlowMethod`, `LowerSlowLength`, `LowerSlowPhase` – slow ROC settings for the lower timeframe.
- `AllowBuyOpen`, `AllowSellOpen` – enable or disable opening longs and shorts.
- `CloseBuyOnTrendFlip`, `CloseSellOnTrendFlip` – force exits when the higher timeframe changes direction.
- `CloseBuyOnLower`, `CloseSellOnLower` – exit when the lower timeframe cross goes against the position.

## Implementation Notes
- The original MQL strategy used a large smoothing library. The StockSharp version maps the supported options to built-in indicators (SMA, EMA, SMMA/RMA, LWMA, Jurik, Kaufman AMA). Unsupported modes (JurX, ParMA, T3, VIDYA) are approximated with the nearest available moving average, so behavior may differ for those combinations.
- Money-management functions, stop-loss, take-profit, and slippage settings from `TradeAlgorithms.mqh` are not reproduced. Instead, the strategy trades with the fixed `Volume` specified in the strategy settings.
- Orders are executed with market orders. Protective logic such as stop-losses or trailing stops can be added via StockSharp protection modules if needed.
- The strategy only trades when both candle subscriptions are fully formed and `IsFormedAndOnlineAndAllowTrading()` returns true.

## Usage Tips
- Choose candle types that correspond to the original trading style (e.g., 6h/30m for swing trading). Other combinations are possible.
- Tune the ROC periods and smoothing methods to match the preferred responsiveness. Jurik smoothing keeps the behaviour closest to the source script.
- Consider adding explicit risk management (stop-loss, position sizing) when running on live accounts, since the port uses simple market exits.
