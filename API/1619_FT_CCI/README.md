# FT CCI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the MetaTrader 5 expert advisor "FT_CCI (barabashkakvn's edition)". It uses the Commodity Channel Index (CCI) to capture sharp reversals once the oscillator stretches far away from its mean. The system mirrors the original logic: when CCI pierces the lower band it flips long, and when it pierces the upper band it flips short. Optional stop-loss and take-profit values are entered in pips and automatically converted into price offsets.

## Overview
- **Core indicator**: Commodity Channel Index with a configurable averaging period (default 14).
- **Bias**: Symmetric long/short. The strategy always holds at most one net position and reverses on opposite signals.
- **Execution**: Market orders on the close of finished candles from the selected timeframe.
- **Risk management**: Optional stop-loss and take-profit distances expressed in pips. If either value is zero the corresponding protection is disabled.
- **Default timeframe**: 30-minute candles (mirrors the "Period()" selection in the original expert).

## How it works
### Long setup
1. Subscribe to finished candles of the selected timeframe.
2. Update the CCI indicator with typical price values.
3. When the latest CCI value is at or below the configured lower threshold (default -210):
   - Close any open short exposure.
   - Enter or add to a long position using the configured trade volume.
4. Maintain the position until either an opposite short setup triggers, a stop-loss/take-profit event occurs, or the strategy is stopped manually.

### Short setup
1. Monitor the same CCI values on finished candles.
2. When the indicator is at or above the upper threshold (default +210):
   - Close any open long exposure.
   - Enter or add to a short position using the configured volume.
3. Hold the short until an opposite long condition fires or protective orders close the trade.

### Trade management
- Stop-loss and take-profit distances are defined in pips. The strategy multiplies them by the detected pip size (price step, multiplied by 10 for 3- and 5-digit forex symbols) to obtain an absolute price offset before enabling StockSharp's built-in `StartProtection`.
- Because the protection is applied once on start, any new position immediately inherits the same stop and target values relative to its fill price.
- Position flips are executed via market orders sized at `configured volume + |current position|`, ensuring that reversing a position both closes the current exposure and opens the new one in a single transaction.

## Parameters
| Name | Description |
| --- | --- |
| **Candle Type** | Timeframe used for calculations and signal generation. |
| **Trade Volume** | Lot size for new positions. Used together with the current position value to size reversal trades. |
| **CCI Period** | Averaging length of the Commodity Channel Index. |
| **CCI Upper Threshold** | CCI level that triggers short entries. |
| **CCI Lower Threshold** | CCI level that triggers long entries. |
| **Stop Loss (pips)** | Distance to the protective stop in pips. Set to 0 to disable. |
| **Take Profit (pips)** | Distance to the profit target in pips. Set to 0 to disable. |

All parameters support optimization through StockSharp's parameter manager.

## Recommended usage
- Works best on liquid forex pairs and indices where 30-minute to 4-hour candles produce pronounced CCI extremes.
- Thresholds of ±210 recreate the FT_CCI defaults. Lower values make the system more reactive; higher values focus on only the most extreme reversals.
- Ensure the security metadata exposes a valid `PriceStep`. The pip converter relies on this value to translate pips into price offsets.
- The strategy assumes a netting account model (single net position). For hedging accounts set the trading volume appropriately so that reversals fully flatten the previous trade.

## Notes
- The indicator must be fully formed before any trade signal is considered. Early candles are ignored until the CCI has enough data to emit valid values.
- Stop-loss and take-profit orders are optional. Leaving them at zero reproduces the original expert advisor behaviour that relied solely on opposite signals for exits.
- Add the strategy to a chart in StockSharp to visualize candles, the CCI indicator, and executed trades; these visual aids are enabled automatically in the C# implementation.
