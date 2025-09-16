# MACD Waterline Cross Expectator Strategy

This strategy goes long when the MACD signal line crosses above the zero level and goes short when it crosses below. Risk management uses a stop loss and a configurable risk‑reward multiplier to set the take profit distance.

## Logic
- Calculate the MACD indicator with configurable fast EMA, slow EMA and signal periods.
- Track the signal line value on each finished candle.
- When the signal line crosses from negative to positive and the strategy is ready to buy, a long market order is placed.
- When the signal line crosses from positive to negative and the strategy is ready to sell, a short market order is placed.
- Protective stop loss and take profit levels are set automatically for each new position.

## Parameters
- **FastEmaPeriod** – length of the fast EMA used in MACD.
- **SlowEmaPeriod** – length of the slow EMA used in MACD.
- **SignalPeriod** – length of the signal line EMA.
- **StopLoss** – distance to the stop loss in absolute price units.
- **Volume** – order size used for new positions.
- **RiskBenefitRatio** – preset ratios from 1:5 to 1:1 which define take profit distance.
- **CandleType** – time frame of candles used by the strategy.

## Notes
- The strategy alternates between long and short trades using an internal flag.
- Trades are executed at market prices and always close and reverse the current position.
