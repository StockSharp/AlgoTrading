# Crypto Scalper Momentum Strategy

## Overview

The **Crypto Scalper Momentum Strategy** replicates the original MetaTrader "Crypto Scalper" expert advisor by combining Money Flow Index, Momentum, and multi-timeframe MACD filters. It operates on a primary intraday timeframe, confirms short-term momentum on a higher timeframe, and respects a macro trend filter derived from a slow MACD. Multiple risk-management features from the MQL implementation were preserved, including currency-based basket targets, money trailing, break-even stops, and equity drawdown protection.

## Trading Logic

1. **Primary Indicators**
   - Money Flow Index (MFI) on the main timeframe with a 14-period default.
   - MACD on the main timeframe (12/26/9 EMA configuration).
2. **Higher Timeframe Momentum**
   - Momentum indicator calculated on a separate candle series. The absolute distance from the MetaTrader baseline (100) must exceed a configurable threshold.
3. **Macro Trend Filter**
   - A slow MACD evaluated on a macro timeframe (daily by default) prevents trading against the higher trend and forces liquidation when it reverses.
4. **Entry Rules**
   - **Longs**: at least one of the last three MFI values is below the oversold threshold, momentum deviation exceeds the threshold, the primary MACD line is above the signal line, and the macro MACD is bullish.
   - **Shorts**: mirror conditions using overbought thresholds and bearish MACD confirmations.
5. **Exit Rules**
   - Fixed stop-loss and take-profit expressed in pips.
   - Optional trailing stop either via candle extremes or a classic distance-based trail.
   - Break-even move after a configurable favorable excursion.
   - Macro MACD reversal closes existing exposure.
   - Currency targets, percent targets, and trailing profit in money replicate the MQL features.
   - Equity drawdown watchdog closes all trades when the account retraces by a given percentage from the peak.

## Risk Management

- **Stops/Targets**: configurable pip distances with optional enabling.
- **Trailing**: candle-based (using the lowest low/highest high of recent candles) or classic pip trailing.
- **Break-even**: moves stops to lock in profits once the trigger distance is reached.
- **Money Management**: basket take profit in currency, percent of initial equity, and trailing profit in money.
- **Equity Stop**: monitors the highest observed equity and closes trades once the drawdown exceeds the allowed percentage.

## Parameters

| Name | Description |
|------|-------------|
| `CandleType` | Primary candle series used for entries. |
| `MomentumCandleType` | Higher timeframe candles feeding the Momentum indicator. |
| `MacroCandleType` | Slow timeframe candles used for the macro MACD filter. |
| `MfiPeriod` | Length of the Money Flow Index. |
| `MfiOversold` / `MfiOverbought` | Oscillator thresholds (default 30 / 70). |
| `MomentumPeriod` | Momentum length on the higher timeframe. |
| `MomentumThreshold` | Minimum deviation from the 100-line required by the momentum filter. |
| `MomentumReference` | Baseline value (MetaTrader default is 100). |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD parameters on the trading timeframe. |
| `MacroMacdFastPeriod` / `MacroMacdSlowPeriod` / `MacroMacdSignalPeriod` | MACD parameters on the macro timeframe. |
| `TradeVolume` | Volume for each market order (lots). |
| `MaxTrades` | Maximum simultaneous trades per direction (0 = unlimited). |
| `UseStopLoss` / `StopLossPips` | Enable and configure the protective stop. |
| `UseTakeProfit` / `TakeProfitPips` | Enable and configure the protective target. |
| `UseTrailingStop` | Master toggle for trailing logic. |
| `UseCandleTrail` | Switch between candle-extreme trailing and classic trailing. |
| `TrailTriggerPips` / `TrailAmountPips` | Trigger distance and distance kept by the classic trailing stop. |
| `CandleTrailLength` / `CandleTrailBufferPips` | Number of candles and extra buffer for candle-based trailing. |
| `UseBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Break-even activation distance and locked-in profit. |
| `UseMoneyTakeProfit` / `MoneyTakeProfit` | Basket take-profit in account currency. |
| `UsePercentTakeProfit` / `PercentTakeProfit` | Basket take-profit in percent of initial equity. |
| `EnableMoneyTrailing` / `MoneyTrailTarget` / `MoneyTrailStop` | Trailing floating profit in currency. |
| `UseEquityStop` / `EquityRiskPercent` | Equity drawdown guard relative to the observed peak. |
| `ForceExit` | Immediately flatten positions on the next candle close. |

## Notes

- Pip distances are converted with the instrument's `PriceStep`. A fallback of `0.0001` is used if the broker does not provide a price step, matching the point handling in MetaTrader.
- The macro MACD subscription can be pointed to monthly candles to mimic the original EA. Daily candles are the default because monthly bars may not be available in all data feeds.
- All comments inside the code are written in English to comply with the repository rules.
