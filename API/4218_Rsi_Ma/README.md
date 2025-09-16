# 4218 RSI MA Strategy

## Overview
This strategy is a C# port of the original MetaTrader expert advisor located in `MQL/9925`. It recreates the RSI_MA momentum oscillator by combining a classic RSI with the slope of an exponential moving average built on the weighted price `(High + Low + 2 * Close) / 4`. Signals are generated on completed candles only, keeping the behaviour identical to the source implementation.

The script is designed for daily EURUSD candles (D1 timeframe) and opens a single position at a time. Nevertheless, any instrument with a meaningful price step can be used as long as the candle type is configured accordingly.

## Strategy logic
1. **Indicator calculation**
   - A Relative Strength Index with configurable length is calculated on closing prices.
   - An exponential moving average with the same length is calculated on the weighted price.
   - The indicator value equals `RSI * (EMA(current) - EMA(previous)) / pipSize` and is clipped to the `[1, 99]` range.
2. **Long entry**
   - Previous indicator value below the oversold extreme (default 5).
   - Latest indicator value above the oversold activation threshold (default 20).
   - No open position or an existing short position (the short is closed before opening a new long).
3. **Short entry**
   - Previous indicator value above the overbought extreme (default 95).
   - Latest indicator value below the overbought activation threshold (default 80).
   - No open position or an existing long position (the long is closed before opening a new short).
4. **Indicator based exit**
   - Long positions are closed when the indicator drops from above the overbought extreme to below the activation level (95 → 80 by default).
   - Short positions are closed when the indicator rises from below the oversold extreme to above the activation level (5 → 20 by default).
5. **Protective exits**
   - Optional stop-loss, take-profit and trailing stop distances are expressed in pips. Distances are automatically converted into price using the security `PriceStep` (fallback 0.0001).
   - Trailing stop tightening follows the behaviour of the original EA: it activates only after price moves more than the configured distance in the favourable direction.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `RsiPeriod` | RSI and EMA length.|
| `OversoldActivationLevel` | Threshold that confirms a long setup after an oversold extreme. |
| `OversoldExtremeLevel` | Extreme that must be reached before longs are allowed. |
| `OverboughtActivationLevel` | Threshold that confirms a short setup after an overbought extreme. |
| `OverboughtExtremeLevel` | Extreme that must be reached before shorts are allowed. |
| `StopLossPips` | Distance for the protective stop-loss. Enable/disable via `UseStopLoss`. |
| `TakeProfitPips` | Distance for the profit target. Enable/disable via `UseTakeProfit`. |
| `TrailingStopPips` | Distance for the trailing stop. Enable/disable via `UseTrailingStop`. |
| `UseStopLoss` | Activates the stop-loss management. |
| `UseTakeProfit` | Activates the take-profit management. |
| `UseTrailingStop` | Activates trailing stop updates. |
| `UseMoneyManagement` | Enables position sizing based on `RiskPercent`. |
| `RiskPercent` | Portfolio percentage risked per trade when money management is active. |
| `TradeVolume` | Fixed volume used when money management is disabled. |
| `CandleType` | Data type of candles processed by the strategy (default Daily). |

## Usage notes
- Attach the strategy to EURUSD daily candles to reproduce the behaviour of the original EA. Other instruments/timeframes are supported after adjusting `CandleType` and thresholds.
- Only one position is kept open at any time. Entering a new trade automatically closes the opposite direction first.
- Money management falls back to the fixed `TradeVolume` whenever portfolio information is unavailable or the computed volume becomes non-positive.
- Ensure that the security `PriceStep` reflects a pip (0.0001 for most FX pairs). Otherwise adjust the parameters accordingly.

## Risk management
- Stop-loss and take-profit levels are evaluated on each completed candle using candle high/low ranges.
- Trailing stop is updated only when the trade is in profit by more than the configured distance and never moved in an unfavourable direction.
- Indicator-based exits still work even when risk controls are disabled, ensuring graceful degradation similar to the MQL version.
