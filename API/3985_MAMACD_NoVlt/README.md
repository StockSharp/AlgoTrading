# MAMACD No Volatility Strategy

## Overview
MAMACD No Volatility is a direct port of the MetaTrader 4 expert advisor `MAMACD_novlt.mq4`. The strategy combines three moving averages calculated on candle lows and closes with a MACD momentum filter. It waits until the fast EMA drops below (for longs) or rises above (for shorts) two low-based LWMA filters, arms a pending setup, and triggers an entry only after the MACD main line confirms the momentum shift.

## Indicators
- **Fast EMA** (`FastEmaPeriod`) calculated on close prices.
- **First LWMA** (`FirstLowWmaPeriod`) calculated on low prices.
- **Second LWMA** (`SecondLowWmaPeriod`) calculated on low prices.
- **MACD main line** with fast period `FastSignalEmaPeriod` and slow period `SlowEmaPeriod`.

All indicators operate on the timeframe defined by `CandleType` (default: 5-minute candles).

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `FirstLowWmaPeriod` | Period of the first LWMA built from candle lows. | 85 |
| `SecondLowWmaPeriod` | Period of the second LWMA built from candle lows. | 75 |
| `FastEmaPeriod` | Period of the fast EMA built from candle closes. | 5 |
| `SlowEmaPeriod` | Slow EMA period for the MACD calculation. | 26 |
| `FastSignalEmaPeriod` | Fast EMA period for the MACD calculation. | 15 |
| `StopLossPoints` | Stop-loss distance in price steps (0 disables the stop-loss). | 15 |
| `TakeProfitPoints` | Take-profit distance in price steps (0 disables the take-profit). | 15 |
| `TradeVolume` | Order volume used for market entries. | 0.1 |
| `CandleType` | Candle series used for all indicators. | 5-minute timeframe |

## Trading Rules
1. **Arm long setup**: Fast EMA is below both LWMA filters.
2. **Arm short setup**: Fast EMA is above both LWMA filters.
3. **Enter long**:
   - Fast EMA crosses back above both LWMAs,
   - A long setup was armed previously,
   - MACD main line is positive or has increased compared to the previous value,
   - Current net position is not long.
4. **Enter short**:
   - Fast EMA crosses back below both LWMAs,
   - A short setup was armed previously,
   - MACD main line is negative or has decreased compared to the previous value,
   - Current net position is not short.
5. **Risk management**: Optional take-profit and stop-loss are applied automatically through the built-in protection service.

The strategy does not implement a dedicated exit signal; positions are managed by the configured stop-loss/take-profit levels or manual intervention.

## Notes
- The MACD confirmation replicates the MQL logic: the main line must be either above zero or rising (for longs) or below zero or falling (for shorts).
- The LWMA calculations use candle low prices to reflect the original indicator configuration.
- Volume scaling mirrors the original EA by using the `TradeVolume` parameter for every order.
