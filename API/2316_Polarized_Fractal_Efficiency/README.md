# Polarized Fractal Efficiency Strategy

This strategy trades based on the **Polarized Fractal Efficiency (PFE)** indicator. PFE measures the efficiency of price movement and changes sign when momentum shifts.

## Trading Logic

1. Subscribe to candles of the selected timeframe and calculate PFE.
2. If PFE on the previous bar is lower than two bars ago and the current value is higher than the previous one, a long position is opened.
3. If PFE on the previous bar is higher than two bars ago and the current value is lower than the previous one, a short position is opened.
4. Opposite positions are closed before opening new ones.
5. Optional stop loss and take profit protection is enabled.

## Parameters

| Name | Description |
|------|-------------|
| `CandleType` | Candle series used for analysis. |
| `PfePeriod` | Period for calculating the PFE indicator. |
| `SignalBar` | Offset of the bar used to generate signals. |
| `TakeProfit` | Take profit in price steps. |
| `StopLoss` | Stop loss in price steps. |

