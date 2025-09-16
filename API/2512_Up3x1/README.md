# Up3x1 Strategy (MT5 Conversion)

## Overview
- Conversion of the MetaTrader 5 expert advisor `up3x1.mq5` into the StockSharp high-level API.
- Trades a triple exponential moving average (EMA) crossover with stop loss, take profit and trailing stop management.
- Processes only finished candles to emulate the original `iTickVolume(0) > 1` guard that forced one decision per bar.
- Default candle series is 1 hour, but the timeframe is configurable through the `CandleType` parameter.

## Trading Logic
1. **Indicators**
   - Fast EMA (`FastPeriod`, default 24).
   - Medium EMA (`MediumPeriod`, default 60).
   - Slow EMA (`SlowPeriod`, default 120).
2. **Long entry**
   - Previous bar: fast EMA below medium EMA and medium below slow (`EMAfast₍t-1₎ < EMAmedium₍t-1₎ < EMAslow₍t-1₎`).
   - Current bar: medium EMA below fast EMA while fast stays below slow (`EMAmedium₍t₎ < EMAfast₍t₎ < EMAslow₍t₎`).
3. **Short entry**
   - Previous bar: fast EMA above medium EMA and medium above slow (`EMAfast₍t-1₎ > EMAmedium₍t-1₎ > EMAslow₍t-1₎`).
   - Current bar: medium EMA crosses above fast EMA while both remain above the slow EMA (`EMAmedium₍t₎ > EMAfast₍t₎ > EMAslow₍t₎`).
4. **Exit logic for both directions**
   - Take profit when price advances by `TakeProfitOffset` from the entry (using candle high for longs, low for shorts).
   - Stop loss when price retraces by `StopLossOffset` from the entry (using candle low for longs, high for shorts).
   - Trailing stop activates once the position moves in favor by more than `TrailingStopOffset` and then follows price at that fixed distance, evaluated on candle extremes.
   - Fallback exit when the fast EMA crosses back below the medium EMA while both stay above the slow EMA (mirrors the `ma_one_1 > ma_two_1 > ma_three_1` check from the MQL version).

## Position Sizing and Risk Management
- `RiskFraction` (default 0.02) multiplies the current portfolio value to approximate the original `FreeMargin * 0.02 / 1000` lot sizing.
- `BaseVolume` (default 0.1) acts as a fallback whenever portfolio data is unavailable or the calculated size becomes non-positive.
- After more than one losing exit the volume is reduced by `volume * losses / 3`, mimicking the cumulative `losses` counter from the script (the counter is not reset after profitable trades, as in the original code).
- Volumes are rounded down to `Security.VolumeStep`, clamped by `Security.MinVolume` / `Security.MaxVolume`, and dropped to zero if the instrument minimum cannot be met.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `FastPeriod` | 24 | Length of the fastest EMA.
| `MediumPeriod` | 60 | Length of the medium EMA.
| `SlowPeriod` | 120 | Length of the slow EMA used as long-term trend filter.
| `TakeProfitOffset` | 0.015 | Absolute price distance for the take profit order (adapt to instrument quoting).
| `StopLossOffset` | 0.01 | Absolute price distance for the stop loss order.
| `TrailingStopOffset` | 0.004 | Trailing distance that locks gains once price advances sufficiently; set to 0 to disable.
| `BaseVolume` | 0.1 | Fallback trade size when dynamic sizing cannot be computed.
| `RiskFraction` | 0.02 | Fraction of portfolio value applied to the dynamic sizing formula.
| `CandleType` | 1 hour time frame | Candle series used for indicator calculations and decision making.

## Conversion Notes
- Trailing stop and protective exits use candle highs/lows instead of raw ticks because the high-level API processes completed candles; this keeps the behaviour deterministic across backtests and live runs.
- Stop loss and take profit are executed via market flattening commands at the evaluated threshold rather than by placing separate protective orders, ensuring compatibility with the high-level strategy flow.
- Dynamic position sizing relies on `Portfolio.CurrentValue`. When unavailable the strategy falls back to `BaseVolume`, similar to the original `LotCheck` fallback to the manual `Lots` input.
- The `losses` counter is intentionally cumulative (never reset on winning trades) to follow the MQL implementation.
- All comments are in English as required by project guidelines.

## Usage Tips
1. Attach the strategy to a security and portfolio, then configure `CandleType` to match the chart resolution you want to emulate from MT5.
2. Review price offsets so they reflect your instrument tick size (e.g., for a 5-digit Forex pair 0.015 equals 150 points as in the source expert).
3. Tune `RiskFraction` / `BaseVolume` to achieve realistic position sizes relative to your account.
4. Optional: disable trailing by setting `TrailingStopOffset` to zero.
5. Monitor logs for messages such as "Enter long" or "Exit short" which mirror the MetaTrader `Print` diagnostics.

## Repository Structure
```
API/2512_Up3x1/
├── CS/Up3x1Strategy.cs      # Converted C# strategy
├── README.md                # English documentation (this file)
├── README_cn.md             # Chinese translation
└── README_ru.md             # Russian translation
```

## Disclaimer
Trading involves significant risk. This example is provided for educational purposes and should be validated on historical and simulated data before any live deployment.
