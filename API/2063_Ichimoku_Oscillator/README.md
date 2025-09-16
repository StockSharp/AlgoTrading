# Ichimoku Oscillator Strategy

The **Ichimoku Oscillator** strategy uses a custom oscillator derived from the Ichimoku indicator. The oscillator is defined as the difference between the lagging line and Senkou Span B minus the difference between Tenkan-sen and Kijun-sen. The resulting value is smoothed with a Jurik moving average.

The strategy enters positions when this smoothed oscillator changes direction and crosses its previous value, attempting to capture emerging trends.

## How it Works
- **Long Entry**: The oscillator is rising and the current value crosses above the previous value. Any short position is closed before opening the long.
- **Short Entry**: The oscillator is falling and the current value crosses below the previous value. Any long position is closed before opening the short.
- Optional stop loss and take profit in percent are applied for risk management.

## Parameters
- **Tenkan Period** – Tenkan-sen period of the Ichimoku indicator.
- **Kijun Period** – Kijun-sen period of the Ichimoku indicator.
- **Senkou Span B Period** – Senkou Span B period of the Ichimoku indicator.
- **Smoothing Period** – Period for the Jurik moving average smoothing the oscillator.
- **Candle Type** – Timeframe used for calculations.
- **Stop Loss %** – Stop loss expressed in percent.
- **Enable Stop Loss** – Enables or disables stop loss protection.
- **Take Profit %** – Take profit expressed in percent.

## Indicators
- Ichimoku
- Jurik Moving Average

## Notes
This strategy is intended for educational purposes and should be tested on historical data before real trading.
