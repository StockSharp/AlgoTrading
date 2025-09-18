# MACD Divergence RSI Strategy

## Overview
- Port of the MetaTrader expert advisor **"Macd diver rsi mt4"** to the StockSharp high-level API.
- Trades a single symbol using RSI filters combined with MACD divergence recognition to time reversals.
- Only one market position can be open at a time; the strategy waits for the flat state before issuing a new signal.

## Signal Logic
1. Every finished candle from the selected timeframe feeds four indicators bound to the strategy:
   - Two independent `RelativeStrengthIndex` instances (for oversold and overbought filters) sampled one bar back.
   - Two `MovingAverageConvergenceDivergence` indicators with configurable fast/slow EMA and signal lengths.
2. **Bullish setup**
   - The previous bar RSI must be below the configurable oversold threshold.
   - The most recent MACD values must form a local dip below a dynamic threshold (equivalent to 3 pips in the current instrument).
   - Historical data is scanned to locate an earlier MACD dip and the associated price swing low. Divergence is confirmed when
     the MACD trough rises while price makes a lower low (regular divergence) or the MACD trough falls while price makes a higher
     low (hidden divergence), matching the original MQL logic.
   - When confirmed and the strategy has no open position, a market buy is sent with direction-specific volume and risk settings.
3. **Bearish setup** mirrors the bullish rules with the RSI overbought filter and MACD peaks. Divergence is validated by
   comparing previous swing highs against the current one.
4. Immediately after an entry the strategy converts the configured stop-loss and take-profit distances from pips to price units
   (respecting the original point-format rules) and applies them through `SetStopLoss` / `SetTakeProfit`.

## Parameters
- `LowerRsiPeriod`, `LowerRsiThreshold` – map to `inp1_Lo_RSIperiod` / `inp1_Ro_Value`.
- `BullishFastEma`, `BullishSlowEma`, `BullishSignalSma` – map to `inp2_fastEMA` / `inp2_slowEMA` / `inp2_signalSMA`.
- `BullishVolume`, `BullishStopLossPips`, `BullishTakeProfitPips` – map to `inp3_VolumeSize`, `inp3_StopLossPips`, `inp3_TakeProfitPips`.
- `UpperRsiPeriod`, `UpperRsiThreshold` – map to `inp4_Lo_RSIperiod` / `inp4_Ro_Value`.
- `BearishFastEma`, `BearishSlowEma`, `BearishSignalSma` – map to `inp5_fastEMA` / `inp5_slowEMA` / `inp5_signalSMA`.
- `BearishVolume`, `BearishStopLossPips`, `BearishTakeProfitPips` – map to `inp6_VolumeSize`, `inp6_StopLossPips`, `inp6_TakeProfitPips`.
- `CandleType` – timeframe source for all calculations.

## Implementation Notes
- The MACD divergence threshold is derived from the current instrument point size and equals 3 pips, matching the 0.0003 default
  used by the MQL version.
- Candle, MACD and price history are stored in bounded lists (600 elements) to reproduce the divergence scanning windows without
  allocating large arrays.
- The strategy uses `SubscribeCandles(...).Bind(...)` to update all indicators in a single pass and processes only finished
  candles, just like the original once-per-bar block execution.
- Pip distances are converted into absolute price offsets before calling `SetStopLoss` and `SetTakeProfit`, reproducing the
  point format rules declared at the top of the MQL source.
