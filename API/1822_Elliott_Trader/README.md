# Elliott Trader Strategy

A strategy that opens layered positions when the Stochastic oscillator reaches extreme values on four-hour candles. It places an initial market order followed by a grid of limit orders. Positions are closed once a profit target is reached and the trend is confirmed by moving averages and Bollinger Bands.

## Entry Rules
- Use Stochastic oscillator (%K length 21, smoothing 3) on H4 candles.
- When %K ≥ **Overbought** level:
  - Sell at market.
  - Place up to eight additional `SellLimit` orders above the current price at configured pip distances.
- When %K ≤ **Oversold** level:
  - Buy at market.
  - Place up to eight additional `BuyLimit` orders below the current price at configured pip distances.

## Exit Rules
- Realized profit reaches **ProfitTarget** and price confirms trend:
  - Long positions exit when price is above the lower Bollinger Band and the 200‑period SMA is above the 55‑period SMA.
  - Short positions exit when price is below the upper Bollinger Band and the 200‑period SMA is below the 55‑period SMA.
- Pending buy limits are cancelled when %K ≥ 90 and the 200‑period SMA ≤ 55‑period SMA.
- Pending sell limits are cancelled when %K ≤ 10 and the 200‑period SMA ≥ 55‑period SMA.

## Parameters
- `StochLength` – %K period for Stochastic.
- `OverboughtLevel` – level to start selling.
- `OversoldLevel` – level to start buying.
- `ProfitTarget` – realized profit required to close open positions.
- `Order2Offset` … `Order9Offset` – pip distances for additional limit orders.
- `CandleType` – timeframe of candles, default 4 hours.

## Indicators
- StochasticOscillator
- BollingerBands
- SMA (200 and 55)
