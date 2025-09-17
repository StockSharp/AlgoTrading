# Fraktrak XonaX Advanced Strategy

## Overview

This strategy is a C# conversion of the MetaTrader 5 expert advisor **Fraktrak xonax.mq5**. The original robot tracks Williams fractals and opens trades when price breaks through the most recent fractal level. The StockSharp version keeps the same idea while leveraging high-level API features such as candle subscriptions, built-in money management helpers, and automatic trade protection.

## Trading Logic

1. **Fractal detection** – the algorithm maintains a five-candle window. When the middle candle creates a higher high (or lower low) than its neighbours, the price is saved as the latest upper (or lower) fractal.
2. **Breakout signals** – when a finished candle touches or exceeds the current fractal level, the strategy prepares to trade:
   - Upper fractal breakout → open a long position (or a short position when *Reverse Mode* is enabled).
   - Lower fractal breakout → open a short position (or a long position when *Reverse Mode* is enabled).
3. **Position management** – the converted strategy reproduces the MetaTrader behaviour:
   - Optional closing of the opposite position before opening a new one.
   - Initial stop-loss and take-profit are set according to the configured pip distances.
   - A two-stage trailing stop moves the protective level after the price advances by the specified *Trailing Step*.
4. **Money management** – choose between a fixed lot or equity-based risk percentage. When risk mode is active, the algorithm estimates the volume using portfolio equity, price step size, and the configured stop distance.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `StopLossPips` | Stop-loss distance expressed in pips. Set to zero to disable the stop-loss level. |
| `TakeProfitPips` | Take-profit distance in pips. Zero disables the target. |
| `TrailingStopPips` | Base trailing stop distance. Requires `TrailingStepPips` to be greater than zero. |
| `TrailingStepPips` | Additional distance that price must move before the trailing stop is advanced. |
| `ReverseMode` | Invert the breakout rules (sell upper fractals, buy lower fractals). |
| `CloseOpposite` | When true, any opposite position is closed before a new trade is opened. |
| `ManagementMode` | Select between `FixedLot` or `RiskPercent` money management. |
| `ManagementValue` | Value used by the active money management mode (lot size or percentage). |
| `CandleType` | Candle series used for both fractal detection and trading decisions. |

## Usage Notes

- The pip size is derived automatically from the instrument price step. Assets with three or five decimal digits are treated as fractional pip instruments (0.1 pip). Adjust the pip parameters accordingly.
- Trailing stop logic matches the original expert: it requires both the trailing distance and the additional step to be positive. Otherwise, trailing is skipped.
- Money management in risk mode assumes that price step cost is available. If it is missing, the strategy falls back to a simplified calculation based on the raw price distance.
- Enable *Close Opposite* to emulate the expert advisor behaviour where a new breakout closes the running trade before entering in the opposite direction.

## Files

- `CS/FraktrakXonaxAdvancedStrategy.cs` – implementation of the strategy.
- `README.md` – current document.
- `README_ru.md` – Russian description.
- `README_cn.md` – Chinese description.
