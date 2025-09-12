# Rawstocks 15 Minute Model Strategy

Rawstocks 15 Minute Model uses swing order blocks and Fibonacci retracement levels to trade within a daily session.

## How it works
- Detects swing highs and lows with an ATR filter.
- Builds bullish and bearish order blocks and computes 61.8% and 79% Fibonacci levels.
- Enters long when price touches a bullish order block and closes above a Fibonacci level before the entry cutoff time.
- Enters short when price tests a bearish order block and closes below a Fibonacci level.
- Closes all positions at 16:30 ET.

## Parameters
- Start Hour
- Start Minute
- Last Entry Hour
- Last Entry Minute
- Force Close Hour
- Force Close Minute
- Fib Level (%)
- Min Swing Size (%)
- Risk/Reward

### Indicators
- Average True Range
