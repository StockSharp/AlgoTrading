# Burg Extrapolator Strategy

## Overview

The Burg Extrapolator Strategy replicates the MetaTrader expert advisor "Burg Extrapolator" using the StockSharp high-level API. The system applies an autoregressive (AR) model solved with the Burg method to forecast future open prices. Trading decisions are driven by the amplitude of the forecast path: when the predicted distance between future highs and lows exceeds configured thresholds, the strategy either opens or closes positions.

## Core Logic

1. **Data preparation**
   - Collects `PastBars` open prices on every finished candle.
   - Optionally transforms the series into logarithmic momentum or rate of change values.
   - Normalizes prices by subtracting the rolling average when raw prices are used.
2. **Autoregressive modelling**
   - Estimates AR coefficients through the Burg method with an order determined by `ModelOrderFraction`.
   - Extrapolates several steps ahead (forecast horizon = `PastBars - order - 1`) and reconstructs price predictions.
3. **Signal generation**
   - Tracks the maximum and minimum predicted prices.
   - If the forecast swing exceeds `MinProfitPips`, generates an entry signal in the respective direction.
   - If the forecast swing exceeds `MaxLossPips`, issues an exit signal for existing positions.
4. **Order execution**
   - Positions are opened with market orders using the calculated risk-based volume.
   - When a stop or opposite signal occurs, the strategy closes positions with market orders.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `RiskPercent` | Equity percentage risked per trade. Used to size orders when a stop-loss distance is available. |
| `MaxPositions` | Maximum cumulative volume expressed as multiples of the order size allowed per direction. |
| `MinProfitPips` | Minimum predicted profit swing (in pips) required to open new positions. |
| `MaxLossPips` | Maximum permitted forecast drawdown (in pips) that will trigger position exits. |
| `TakeProfitPips` | Static take-profit distance (in pips). Set to zero to disable. |
| `StopLossPips` | Static stop-loss distance (in pips). Required for risk sizing. |
| `TrailingStopPips` | Trailing stop distance (in pips). Works only when stop-loss is enabled. |
| `PastBars` | Number of historical bars used as input to the Burg model. |
| `ModelOrderFraction` | Fraction of `PastBars` that defines the AR order (integer truncation). |
| `UseMomentum` | Enables logarithmic momentum preprocessing (`log(p[i]/p[i-1])`). |
| `UseRateOfChange` | Enables rate of change preprocessing (`p[i]/p[i-1]-1`) when momentum is disabled. |
| `OrderVolume` | Fallback order size when risk-based sizing cannot be calculated. |
| `CandleType` | Data type (timeframe) of candles used for calculations. |

## Trading Rules

- **Entry**: When the forecasted path indicates a swing greater than `MinProfitPips`, open a long position if the highest projected price comes first, or open a short position if the lowest projection appears first.
- **Exit**: Close positions when the forecast swing exceeds `MaxLossPips` or when the opposite entry signal is detected.
- **Protection**: Uses `StartProtection` to configure optional stop-loss, take-profit, and trailing stop in absolute price units derived from pips.
- **Position sizing**: If both `StopLossPips` and `RiskPercent` are positive, the trade volume is computed as `risk_amount / (stop_distance)`. Otherwise, `OrderVolume` is used.

## Implementation Notes

- Works exclusively with finished candles to avoid look-ahead bias.
- Avoids indicator `GetValue` calls by processing values directly within the `Bind` callback.
- Respects StockSharp's high-level API conventions, using `SubscribeCandles` and `StartProtection` for risk management.
- Trailing logic mirrors the original EA by enabling platform-managed trailing stops.

## Usage Tips

- Choose `PastBars` and `ModelOrderFraction` carefully; high orders can lead to overfitting or unstable forecasts.
- The forecast horizon equals `PastBars - order - 1`; ensure that the horizon is at least a few bars by keeping `ModelOrderFraction` below 1.
- Momentum and ROC modes require positive prices. Instruments that can cross zero should stick to raw price mode.
- For markets with fractional pips, the strategy automatically scales pip size using the security's decimals (×10 for 3 or 5 decimals).

## Limitations

- The AR model assumes stationarity; strong trends or regime shifts can reduce accuracy.
- Forecast-based signals are sensitive to noise—consider pairing with additional filters if using live trading.
- Accurate risk sizing requires portfolio valuation and a valid stop-loss distance; otherwise default volumes are used.

## Files

- `CS/BurgExtrapolatorStrategy.cs` – C# implementation of the strategy.
- `README.md` – English documentation (this file).
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.

Python version is intentionally omitted as per task requirements.
