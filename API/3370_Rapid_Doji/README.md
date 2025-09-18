# Rapid Doji Strategy

## Overview

The Rapid Doji strategy replicates the logic of the original "Rapid Doji EA" expert advisor. It scans finished candles of a configurable timeframe (daily by default) and places stop entry orders above and below every doji candle. Protective stops are positioned using an Average True Range (ATR) multiplier, while an auxiliary trailing stop keeps the risk distance fixed in raw points after a position becomes profitable.

## Trading Logic

1. **Data subscription** – the strategy listens to finished candles of the selected timeframe and maintains an ATR indicator with a configurable period.
2. **Doji detection** – a candle is treated as a doji when the absolute body size is at most 3% of the full candle range. Only completed candles are evaluated.
3. **Order placement** – when a valid doji is found:
   - A buy stop order is placed at the doji high.
   - A sell stop order is placed at the doji low.
   - Each entry remembers a protective stop price equal to the opposite extreme minus/plus ATR × multiplier.
4. **Risk management** – once a position is opened the remaining pending order is cancelled, the memorized stop is registered as a protective stop, and the trailing logic takes control.
5. **Trailing stop** – on each new candle the stop level is moved to keep a fixed distance (in points converted through the instrument price step) from the latest closing price, but only when the position is already profitable.

The strategy never uses take-profit targets; exits happen through the protective stop or manual intervention.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle data type used for pattern detection (daily timeframe by default). |
| `AtrPeriod` | Lookback length of the ATR indicator. |
| `AtrMultiplier` | Multiplier applied to the ATR value for stop-loss calculation. |
| `TrailingDistancePoints` | Fixed distance in raw points used when trailing the stop. |

All parameters support optimisation inside the StockSharp environment.

## Implementation Notes

- The code relies on the high-level candle subscription API (`SubscribeCandles`) combined with indicator binding (`Bind`) to avoid manual history handling.
- Orders are normalised through `Security.ShrinkPrice` to respect exchange tick size.
- Protective stops are managed explicitly to mimic the behaviour of the original MetaTrader expert advisor.
- The project intentionally omits a Python implementation as per the task requirements.
