# MACD Volume BBO Reversal Strategy

Strategy combining volume oscillator with MACD zero-line crossings and signal comparison.
Enters long when MACD crosses above zero with positive volume oscillator and MACD above its signal.
Short entries are symmetric. Stop loss uses recent low/high and take profit is based on risk reward ratio.

## Parameters
- `VolumeShortLength` – short EMA period for volume (default: 6)
- `VolumeLongLength` – long EMA period for volume (default: 12)
- `MacdFastLength` – fast MA period for MACD (default: 11)
- `MacdSlowLength` – slow MA period for MACD (default: 21)
- `MacdSignalLength` – signal line period for MACD (default: 10)
- `LookbackPeriod` – bars to calculate recent high/low (default: 10)
- `RiskReward` – take profit to stop loss ratio (default: 1.5)
- `CandleType` – timeframe for candles (default: 5 minutes)
