# Up3x1 Shifted SMA Strategy (MT4 Conversion)

## Overview
- Conversion of the MetaTrader 4 expert advisor `up3x1.mq4` located in `MQL/8097`.
- Implements the triple simple moving average crossover with a positive chart shift exactly as in the original script.
- Processes only completed candles to emulate the `Volume[0] > 1` guard that forced the expert to evaluate once per bar.
- Risk management features include take profit, stop loss, dynamic lot reduction after losing trades and an optional trailing stop.

## Trading Logic
1. **Indicators**
   - Three simple moving averages with a chart shift of 6 bars (fast = 24, medium = 60, slow = 120 by default).
2. **Long entry**
   - Previous bar: `SMAfast₍t-1₎ < SMAmedium₍t-1₎ < SMAslow₍t-1₎`.
   - Current bar: `SMAmedium₍t₎ < SMAfast₍t₎ < SMAslow₍t₎`.
   - Condition replicates `ma1 < ma2 < ma3 && ma5 < ma4 < ma6` from MQL.
3. **Short entry**
   - Previous bar: `SMAfast₍t-1₎ > SMAmedium₍t-1₎ > SMAslow₍t-1₎`.
   - Current bar: `SMAmedium₍t₎ > SMAfast₍t₎ > SMAslow₍t₎`.
4. **Exit rules**
   - Take profit and stop loss respect the configured point distance multiplied by `Security.PriceStep` (or used directly when the step is unknown).
   - Trailing stop locks profits once price advances by more than `TrailingStopPoints` and trails the extreme reached after the entry.
   - Failsafe exit when the moving averages flip to the opposite ordering, mirroring the original `OrderClose` logic.

## Position Sizing
- Default volume equals `BaseVolume` (0.1 lot) whenever portfolio metrics are unavailable.
- When `Portfolio.CurrentValue` exists, the strategy multiplies it by `RiskFraction` (default `0.00002`, equivalent to the MQL formula `FreeMargin * 0.02 / 1000`).
- After more than one losing exit, the volume is reduced by `volume * losses / 3`, exactly like the `LotsOptimized` routine.
- Volume is rounded down to `Security.VolumeStep` and dropped to zero if it cannot satisfy `Security.MinVolume`.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `FastPeriod` | 24 | Length of the fastest shifted SMA. |
| `MediumPeriod` | 60 | Length of the medium shifted SMA. |
| `SlowPeriod` | 120 | Length of the slow shifted SMA. |
| `TakeProfitPoints` | 150 | Distance in price points between the entry price and the take profit. |
| `StopLossPoints` | 100 | Distance in price points between the entry price and the stop loss. |
| `TrailingStopPoints` | 100 | Optional trailing stop distance in points (set to 0 to disable). |
| `BaseVolume` | 0.1 | Fallback trade size and minimum volume after reductions. |
| `RiskFraction` | 0.00002 | Fraction of the portfolio value used to compute dynamic volume. |
| `CandleType` | 1 hour time frame | Candle series used to feed indicators. |

## Conversion Notes
- The strategy uses the high-level API (`SubscribeCandles` + `Bind`) and avoids manual history buffers.
- Indicator values are stored between calls to mimic the `shift` parameter without direct index access.
- Protective exits are executed with market commands at the detected price level to stay compatible with the StockSharp abstraction.
- All inline comments are written in English, complying with project guidelines.

## Usage
1. Attach the strategy to a security and portfolio in StockSharp Designer or code.
2. Select a candle series (`CandleType`) that matches your MT4 timeframe (H1 by default).
3. Review the point-based risk parameters to align with the instrument tick size (e.g., 0.0001 for most Forex pairs).
4. Set `TrailingStopPoints` to zero when trailing is not required.
5. Monitor logs for messages such as "Enter long" and "Exit short" that mirror the MQL diagnostics.

## Repository Structure
```
API/3924/
├── CS/Up3x1ShiftedSmaStrategy.cs  # Converted C# strategy with English comments
├── README.md                      # English documentation (this file)
├── README_cn.md                   # Chinese translation
└── README_ru.md                   # Russian translation
```

## Disclaimer
Trading involves significant risk. The strategy is provided for educational purposes and must be validated on historical and simulated data before live trading.
