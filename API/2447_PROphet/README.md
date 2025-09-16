# PROphet Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The PROphet strategy evaluates price ranges of the last three completed candles to generate signals during specified trading hours. A custom function combines the ranges with user-defined coefficients. If the function is positive, the strategy opens a position in the corresponding direction.

Long trades use coefficients `X1..X4` and a trailing stop defined by `BuyStopPoints`. Short trades use coefficients `Y1..Y4` and `SellStopPoints`. Stops trail when price moves in favor of the position by more than the spread plus twice the stop distance. Positions are closed after 18:00 or when the trailing stop is hit.

## Details

- **Entry Criteria**
  - **Long**: `Qu(X1,X2,X3,X4) > 0` and current hour between 10 and 18.
  - **Short**: `Qu(Y1,Y2,Y3,Y4) > 0` and current hour between 10 and 18.
- **Exit Criteria**
  - **Long**: Hour > 18 or best bid falls below trailing stop.
  - **Short**: Hour > 18 or best ask rises above trailing stop.
- **Parameters**
  - `EnableBuy` – allow opening long positions.
  - `EnableSell` – allow opening short positions.
  - `X1, X2, X3, X4` – coefficients for long signal function.
  - `Y1, Y2, Y3, Y4` – coefficients for short signal function.
  - `BuyStopPoints` – trailing stop distance in points for long trades.
  - `SellStopPoints` – trailing stop distance in points for short trades.
  - `CandleType` – candle type for calculations (default 5‑minute).
- **Filters**
  - Category: Intraday
  - Direction: Both
  - Indicators: None
  - Stops: Trailing stop
  - Complexity: Moderate
  - Timeframe: Short-term
