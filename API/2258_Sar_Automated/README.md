# SAR Automated Strategy

This example demonstrates a simple trading approach based on the **Parabolic SAR** indicator.
The strategy opens a long position when the current price is above the SAR value and opens a short position when price is below the SAR. Additional risk management features include fixed stop-loss, take-profit and an optional trailing stop.

## Parameters
- `SarStep` – acceleration factor for the SAR calculation.
- `SarMax` – maximum acceleration factor for the SAR.
- `StopLoss` – stop-loss distance in price units.
- `TakeProfit` – take-profit distance in price units.
- `TrailingStop` – trailing stop distance in price units.
- `CandleType` – candle type used for indicator calculations.

## Trading Logic
1. Subscribe to candles and calculate Parabolic SAR values.
2. **Entry**:
   - Go long when SAR is below the close price and no position exists.
   - Go short when SAR is above the close price and no position exists.
3. **Exit**:
   - Close the position when price reaches the opposite SAR level.
   - Apply stop-loss, take-profit and trailing stop rules.

This strategy is intended for educational purposes and shows how to use indicators and risk controls with the high-level StockSharp API.
