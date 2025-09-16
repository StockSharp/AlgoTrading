# RSI Bollinger Fractal Breakout Strategy

## Overview
This strategy reproduces the MetaTrader "RSI and Bollinger Bands" expert advisor in StockSharp. It applies Bollinger Bands to the RSI oscillator, waits for a recent fractal breakout level and places stop orders beyond that level with configurable offsets. A Parabolic SAR trailing filter dynamically tightens stops once a position is open.

## Indicators and Signals
- **RSI** (default 8 periods) – the main oscillator. Overbought and oversold thresholds are used to cancel pending orders.
- **Bollinger Bands on RSI** (default 14 periods, 1.0 deviation) – entries only trigger when the RSI closes outside the upper or lower band, matching the original script behaviour where Bollinger is fed by RSI values.
- **Bill Williams Fractals** – the strategy scans the last confirmed up and down fractals (5-bar pattern) and uses their prices as the base breakout levels.
- **Parabolic SAR** (step 0.003, max 0.2) – delivers a trailing stop reference once a position is active.

## Entry Logic
1. Work is performed on finished candles of the selected timeframe (default 4-hour).
2. When an **up fractal** appears and the RSI closes above the **upper Bollinger band**, while the previous close remains below the fractal, a **buy stop** is placed:
   - Entry price = fractal high + indent (15 pips by default).
   - Optional stop loss = entry − StopLossPips.
   - Optional take profit = entry + TakeProfitPips.
3. Symmetrically, when a **down fractal** forms and RSI closes below the **lower Bollinger band**, while the previous close remains above the fractal, a **sell stop** is placed below the fractal.
4. RSI reverting inside the channel cancels pending orders:
   - RSI < lower threshold cancels buy stops.
   - RSI > upper threshold cancels sell stops.

## Exit and Risk Management
- Fixed stop loss and take profit distances (in pips) replicate the MQL inputs. Setting any distance to `0` disables that protection.
- The Parabolic SAR trailing logic requires the SAR to be at least `SarTrailingPips` away from the current price and only moves the stop in the favourable direction.
- When the trailing stop crosses price or the price reaches the fixed take profit the position is closed with a market order.
- Opening a position automatically clears the opposite pending order and stores the intended protective levels.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `RsiPeriod` | RSI smoothing length. | 8 |
| `BandsPeriod` | RSI Bollinger period. | 14 |
| `BandsDeviation` | Standard deviation multiplier for Bollinger on RSI. | 1.0 |
| `SarStep` | Parabolic SAR acceleration step. | 0.003 |
| `SarMax` | Parabolic SAR maximum acceleration. | 0.2 |
| `TakeProfitPips` | Take profit distance in pips. | 50 |
| `StopLossPips` | Stop loss distance in pips. | 135 |
| `IndentPips` | Offset beyond a fractal before placing the stop order. | 15 |
| `RsiUpper` | RSI threshold that cancels sell stops. | 70 |
| `RsiLower` | RSI threshold that cancels buy stops. | 30 |
| `SarTrailingPips` | Minimum gap (in pips) between price and SAR before trailing. | 10 |
| `CandleType` | Data type / timeframe for processing. | 4-hour candles |

## Notes
- Python version is intentionally omitted, as requested.
- Use `Volume` in the base class to configure the lot size (default 1 if unspecified).
- The strategy should be run on the same timeframe as the original EA configuration (EURUSD H4 according to the provided `.set` file).
