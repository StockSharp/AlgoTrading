# CDC PL MFI Strategy

## Overview
The **CDC PL MFI Strategy** reproduces the MetaTrader expert advisor `Expert_ADC_PL_MFI` (MQL/299) in StockSharp. It searches for the **Dark Cloud Cover** and **Piercing Line** two-candle reversal patterns and validates each signal with the **Money Flow Index (MFI)** oscillator. The strategy uses the same indicator periods and level thresholds as the original expert, adds optional stop-loss and take-profit protection in pip units, and closes positions when the MFI crosses configurable reversal levels.

## Trading Logic
1. Subscribe to the configured candle type (one-hour candles by default) and calculate a Money Flow Index with the specified period. Maintain simple moving averages of candle body size and closing prices to replicate the original trend and volatility filters.
2. When a bullish **Piercing Line** pattern forms (gap below the previous low, bullish close above the midpoint of the prior bearish candle, both candles larger than the average body, and the prior close below the trend average) *and* the current MFI value is below the **LongEntryLevel** (default `40`), enter or flip to a long position.
3. When a bearish **Dark Cloud Cover** pattern forms (gap above the previous high, bearish close below the midpoint of the prior bullish candle, both candles larger than the average body, and the prior close above the trend average) *and* the current MFI value is above the **ShortEntryLevel** (default `60`), enter or flip to a short position.
4. Monitor the MFI to close positions proactively:
   - Close short positions when the MFI crosses above **ExitLowerLevel** (`30`) or **ExitUpperLevel** (`70`).
   - Close long positions when the MFI crosses below **ExitUpperLevel** (`70`) or **ExitLowerLevel** (`30`).
5. Protective orders are optional. When **TakeProfitPips** or **StopLossPips** are greater than zero, the strategy calls `StartProtection` with the corresponding price offsets (pip distance multiplied by the security price step).

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle data type used for pattern detection. | `1 hour` time frame |
| `MfiPeriod` | Length of the Money Flow Index oscillator. | `49` |
| `BodyAveragePeriod` | Period of the candle body moving average used to qualify “long” candles. | `11` |
| `LongEntryLevel` | MFI threshold that confirms bullish Piercing Line setups. | `40` |
| `ShortEntryLevel` | MFI threshold that confirms bearish Dark Cloud Cover setups. | `60` |
| `ExitLowerLevel` | Lower MFI level that triggers covering short positions. | `30` |
| `ExitUpperLevel` | Upper MFI level that triggers closing long positions. | `70` |
| `StopLossPips` | Optional stop-loss distance in pips (0 disables protection). | `50` |
| `TakeProfitPips` | Optional take-profit distance in pips (0 disables protection). | `50` |

## Notes
- Volume defaults to `1` lot. When the strategy flips direction it sends a single market order sized to close the existing position and open the new one, matching the MQL behavior.
- Pattern detection mirrors the MetaTrader logic: only completed candles are evaluated, gaps must occur beyond the previous high/low, and a simple moving average enforces the prevailing trend condition.
- The Money Flow Index values come directly from the bound indicator. No manual buffering of indicator history is required; the strategy stores only the most recent values to detect threshold crossings.
- No Python port is provided; only the C# implementation is included in this directory.
