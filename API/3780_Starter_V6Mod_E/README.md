# Starter V6 Mod E

**Starter V6 Mod E** is a high-level StockSharp conversion of the MetaTrader 4 expert advisor `Starter_v6mod_e_www_forex-instruments_info.mq4`. The port keeps the original combination of Laguerre oscillator extremes, dual EMA momentum confirmation, CCI filtering, and EMA-angle gating while adapting execution to StockSharp's event-driven architecture.

## Trading logic

- **Trend gate:** a 34-period EMA slope is measured between configurable start/end shifts. The slope is expressed in pip units; only positive slopes allow long trades, only negative slopes allow shorts, and flat readings block new entries.
- **Laguerre extremes:** a handcrafted Laguerre RSI (gamma = 0.7 by default) tracks oversold/overbought states on the 0–1 scale. Longs require both current and previous values to stay below the `Laguerre Oversold` level, shorts require both values above `Laguerre Overbought`.
- **EMA momentum filter:** 120- and 40-period EMAs (median price) must both rise for longs and both fall for shorts, mirroring the original MA filter.
- **CCI confirmation:** a 14-period CCI must be below `-CCI Threshold` for longs and above `+CCI Threshold` for shorts, replicating the `Alpha` filter from MQL.
- **Friday safety:** new trades are blocked after `Friday Block Hour`, and any remaining positions are liquidated once `Friday Exit Hour` is reached.

## Risk management

- Configurable stop-loss, take-profit, and trailing-stop distances (in pips) emulate the expert's money management block.
- Trailing stops follow the best favorable price after entry and close the trade when retracement exceeds the configured distance.
- Manual position closing is executed through `SellMarket`/`BuyMarket`, ensuring high-level API compliance.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Volume` | Order volume for each market entry. |
| `StopLossPips` | Protective stop distance in pips. |
| `TakeProfitPips` | Profit target in pips. |
| `TrailingStopPips` | Trailing stop distance in pips (0 disables trailing). |
| `SlowEmaPeriod` | Period of the slow EMA calculated on PRICE_MEDIAN. |
| `FastEmaPeriod` | Period of the fast EMA calculated on PRICE_MEDIAN. |
| `AngleEmaPeriod` | EMA period used for the angle detector. |
| `AngleStartShift` / `AngleEndShift` | Bar shifts used to compute EMA slope. |
| `AngleThreshold` | Minimum slope (in pip units) required to allow trading. |
| `CciPeriod` / `CciThreshold` | Period and absolute threshold for the CCI filter. |
| `LaguerreGamma` | Gamma parameter for the Laguerre oscillator. |
| `LaguerreOversold` / `LaguerreOverbought` | Entry thresholds on the 0–1 Laguerre scale. |
| `CandleType` | Candle data type (default 1-minute). |
| `FridayBlockHour` / `FridayExitHour` | Hours (local instrument time) controlling Friday risk limits. |

## Conversion notes

- The Laguerre oscillator is implemented directly from the original recursive formula, keeping the 0–1 output range and gamma smoothing.
- EMA slope replaces the MQL angle helper by computing pip-normalized differences between historical EMA points.
- Money management features such as equity cut-off and grid stacking are intentionally omitted because the MT4 variant being converted disabled them by default and StockSharp encourages explicit portfolio control.
- Orders are sent through `BuyMarket`/`SellMarket` and rely on `OnNewMyTrade` to track fill prices for trailing logic.
