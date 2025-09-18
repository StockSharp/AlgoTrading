# Universum 3.0 Original Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the original **Universum_3_0** MQL4 expert advisor using the StockSharp high-level API.
It combines a simple DeMarker threshold entry model with a martingale-like position sizing rule that adapts
lot size after losing trades.

## Trading Logic

- **Indicator**: classic DeMarker oscillator with configurable period.
- **Signal Generation**:
  - Open a long position when `DeMarker > 0.5` at the close of a finished candle.
  - Open a short position when `DeMarker < 0.5` at the close of a finished candle.
  - Only one position can be active at a time; new signals are ignored while a trade is open.
- **Exit Management**:
  - Protective stop-loss and take-profit levels are attached using absolute price offsets measured in points.
  - Positions are closed automatically by these protective levels; the strategy does not flip immediately.
- **Money Management**:
  - After a profitable trade, volume resets to the base lot.
  - After a losing trade, volume is multiplied by `(TakeProfitPoints + StopLossPoints) / (TakeProfitPoints - SpreadPoints)`.
  - The spread value is taken from live Level1 quotes and converted to "points" using symbol precision.
  - Consecutive losses are counted; reaching the limit stops the strategy to emulate the original loss protection.
  - Setting `FastOptimize = true` disables the adaptive sizing rule and always uses the base lot, which speeds up optimisations.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Time frame used for DeMarker calculations. | 1-minute time frame |
| `DemarkerPeriod` | Look-back period of the DeMarker oscillator. | `10` |
| `TakeProfitPoints` | Take-profit distance expressed in points (converted to absolute price internally). | `50` |
| `StopLossPoints` | Stop-loss distance expressed in points. | `50` |
| `BaseVolume` | Initial trading volume used after each profitable trade. | `1` |
| `LossesLimit` | Maximum number of consecutive losses before the strategy stops. | `1,000,000` |
| `FastOptimize` | When `true` disables adaptive sizing for fast optimisation passes. | `true` |

## Implementation Notes

- Level1 data is required to estimate the current spread and replicate the original lot multiplier.
- Volume normalisation honours the instrument's minimum volume, maximum volume and step size.
- Stop-loss and take-profit offsets automatically adapt to 3/5 digit instruments by adjusting the point size.
- The chart visualisation plots candles, the DeMarker indicator and executed trades for easier validation.

## Usage Tips

1. Provide Level1 bid/ask data in addition to candles to ensure the spread-based multiplier works correctly.
2. Use `FastOptimize = true` during coarse parameter searches, then disable it for precise backtests and live trading.
3. Monitor the consecutive loss counter when running with aggressive multipliers to avoid exceeding broker limits.
4. Adjust `TakeProfitPoints` and `StopLossPoints` to match the original symbol or your risk profile before trading live.
