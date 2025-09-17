# Accelerator Trailing TP & SL Strategy

## Overview
The Accelerator Trailing TP & SL strategy ports the "Accelerator Trailing TP&SL" Expert Advisor from MetaTrader to the StockSharp high-level API. The system blends Bill Williams' Accelerator Oscillator with multi-timeframe momentum confirmation and a monthly MACD trend filter. Entries are layered with geometric position sizing while exits combine classic stop/target distances, adaptive trailing and break-even logic.

## Trading Logic
- **Momentum filter** – a 14-period Momentum indicator calculated on a higher timeframe must deviate from the neutral 100 level by at least the configured threshold on any of the last three completed bars.
- **Accelerator Oscillator** – long trades require a positive accelerator reading, short trades require a negative reading on the signal timeframe.
- **Moving averages** – a fast linear weighted moving average (LWMA) must be above the slow LWMA for longs and below it for shorts, approximating the original fast/slow trend filter.
- **Monthly MACD trend** – by default the filter observes monthly candles. Long trades demand the MACD line to be above the signal line (even when both values are negative), while short trades require the opposite condition.
- **Layered entries** – the strategy can pyramid up to the configured maximum number of positions per direction. Each additional entry is multiplied by the lot exponent, recreating the martingale-style sizing used in the MQL program.

## Risk Management
- **Static stop loss / take profit** – distances in pips mirror the original Stop Loss and Take Profit settings.
- **Trailing stop** – when enabled the strategy trails the most favorable price by the configured number of pips.
- **Break-even move** – after a trade reaches the trigger distance the stop is advanced by the specified offset, protecting accumulated profits.
- **MACD exit** – when the MACD filter flips against the active position the strategy can close all positions immediately, matching the manual exit helper in the MQL code.

## Parameters
| Parameter | Description |
| --- | --- |
| `FastMaLength` / `SlowMaLength` | Periods of the fast and slow LWMAs on the trading timeframe. |
| `MomentumThreshold` | Minimum absolute deviation of momentum from the neutral 100 value on the higher timeframe. |
| `StopLossPips` / `TakeProfitPips` | Protective stop and target distances in pips. |
| `TrailingStopPips` | Distance used by the optional trailing stop manager. |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Defines when and how the stop is moved to break-even. |
| `MaxTrades` | Maximum number of layered entries per direction. |
| `BaseVolume` | Volume of the first order in a sequence. |
| `LotExponent` | Multiplier applied to each additional layered entry. |
| `EnableTrailing` | Enables or disables trailing-stop management. |
| `UseBreakEven` | Enables or disables the break-even stop movement. |
| `CloseOnMacdFlip` | Closes all trades if the higher timeframe MACD reverses. |
| `CandleType` | Primary candle series for signals (defaults to 15 minutes). |
| `MomentumCandleType` | Higher timeframe candles used by the momentum filter (defaults to 1 hour). |
| `MacdCandleType` | Candle series used for the MACD trend filter (defaults to monthly candles). |

## Notes
- The strategy relies on the instrument `PriceStep` to convert pip-based risk settings to price distances. Please ensure the security metadata is populated when running the strategy.
- Because StockSharp uses net positions, additional layered entries are opened by repeatedly sending market orders until the configured maximum is reached. Exits close the entire net position, matching the "close all" routines in the original expert.
- The monthly MACD timeframe can be adjusted through the `MacdCandleType` parameter to suit different instruments or backtests.
