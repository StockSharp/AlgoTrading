# Orb Vwap Braid Filter Strategy

Opening range breakout strategy using VWAP and Braid filter confirmation.

## Rules
- Trades between 09:35 and 11:00 exchange time
- One trade per day
- Long when price closes above the opening range high, above VWAP and Braid filter is bullish
- Short when price closes below the opening range low, below VWAP and Braid filter is bearish
- Stop loss at the opposite side of the range
- Take profit at two times risk limited by previous day or pre-market levels

## Indicators
- Volume Weighted Moving Average
- Exponential Moving Average (3, 7, 14)
- Average True Range (14)
