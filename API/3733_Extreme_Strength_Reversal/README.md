# Extreme Strength Reversal Strategy

## Summary
- Counter-trend system converted from the MetaTrader EXSR expert advisor.
- Combines Bollinger Bands and RSI extremes to locate exhaustion moves.
- Uses percent-based position sizing with fixed stop-loss and take-profit in pips.

## Trading Logic
1. Subscribe to the configured candle series (defaults to 1-hour candles).
2. Calculate a Bollinger Bands envelope (period, deviation) and an RSI oscillator.
3. When a candle closes:
   - A long setup requires: RSI below the oversold level yet above zero, the candle low below the lower band, and a bullish body (close above open).
   - A short setup requires: RSI above the overbought level, the candle high above the upper band, and a bearish body (close below open).
4. Only one position may be open at a time. Opposite exposure is closed before reversing.
5. Stops and targets are projected from the fill price using MetaTrader-style pips. The engine monitors subsequent candles and exits when either level is touched.

## Money Management
- Order size defaults to the strategy's `Volume` property. When it is zero the strategy derives the volume from `RiskPercent` and the stop distance.
- Risk is computed from current portfolio equity (fallback to balance/begin value). Stop distance is translated into price or monetary units using the instrument's step and step price.
- Volume is normalized to the instrument's volume step, minimum and maximum constraints.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| Risk Percent | Percentage of equity risked per trade. | 1% |
| Stop Loss (pips) | Stop distance in MetaTrader pips. | 150 |
| Take Profit (pips) | Take-profit distance in pips. | 300 |
| Bollinger Period | Candles used for Bollinger Bands. | 20 |
| Bollinger Deviation | Standard deviation multiplier. | 2.0 |
| RSI Period | Candles used for RSI. | 14 |
| RSI Overbought | RSI level considered extremely overbought. | 80 |
| RSI Oversold | RSI level considered extremely oversold. | 20 |
| Candle Type | Candle timeframe for the analysis. | 1 hour |

## Notes
- Ensure the selected symbol exposes price step, step price and volume step for precise sizing. The strategy falls back to reasonable defaults when unavailable.
- Risk management triggers even when trading is temporarily disabled, so protective exits remain active.
- The logic processes only completed candles, mirroring the original EA that works on the previous bar.
