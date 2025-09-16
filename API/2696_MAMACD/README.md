# MAMACD Strategy

## Overview
This strategy is a direct conversion of the MetaTrader 5 expert advisor **MAMACD (barabashkakvn's edition)** from the `MQL/19334` folder into StockSharp's high-level API. The approach combines trend detection on low prices through two linear weighted moving averages (LWMA) with a fast exponential moving average (EMA) trigger and confirmation from the MACD main line. Trading is performed once per finished candle and keeps the logic of the original EA, including the reset flags that require the fast EMA to leave the LWMA channel before a new entry is allowed.

## Indicators
- **LWMA #1 (Low price, default 85)** – slow baseline filter applied to candle lows.
- **LWMA #2 (Low price, default 75)** – slightly faster filter on candle lows for channel confirmation.
- **EMA Trigger (Close price, default 5)** – momentum trigger that has to cross above/below both LWMAs to arm a trade.
- **MACD main line (fast 15, slow 26)** – confirmation filter; longs require positive or rising MACD, shorts require negative or declining MACD.

## Entry Logic
1. The strategy waits for completed candles only (`CandleStates.Finished`).
2. When the trigger EMA drops below both LWMAs, a **long-ready flag** is set. A long position may be opened once the EMA comes back above both LWMAs **and** MACD is either above zero or greater than its previous value. Only one long position can be opened at a time.
3. When the trigger EMA rises above both LWMAs, a **short-ready flag** is set. A short position may be opened after the EMA returns below both LWMAs and MACD is either below zero or smaller than its previous value. Only one short position is active at a time.
4. Position sizing uses the strategy `Volume` property. When switching direction the algorithm closes the opposite exposure first.

## Exit Logic
- No discretionary exit logic is coded in the original EA. Protective orders are handled through StockSharp's `StartProtection` with optional stop-loss and take-profit distances measured in pips. Hitting either protection closes the position automatically.

## Parameters
| Name | Description |
| --- | --- |
| `FirstLowMaLength` | Period of the first LWMA applied to low prices (default 85). |
| `SecondLowMaLength` | Period of the second LWMA applied to low prices (default 75). |
| `TriggerEmaLength` | Period of the fast EMA trigger on closing prices (default 5). |
| `MacdFastLength` | Fast EMA length of the MACD main line (default 15). |
| `MacdSlowLength` | Slow EMA length of the MACD main line (default 26). |
| `StopLossPips` | Stop-loss distance in pips; set to zero to disable (default 15). |
| `TakeProfitPips` | Take-profit distance in pips; set to zero to disable (default 15). |
| `CandleType` | Time frame of candles processed by the strategy (default 1 hour). |

## Implementation Notes
- Pip size is derived from `Security.PriceStep`. For 3- and 5-digit symbols the code automatically multiplies the step by 10 to mimic the MT5 definition of a pip.
- The MACD history buffer matches the EA: the very first valid MACD value is stored and used as reference for the following bar before signals are evaluated.
- Flags `_readyForLong` and `_readyForShort` replicate the original `startb`/`starts` state machine, ensuring that price has to leave the LWMA channel before any new trade is taken.
- Chart areas visualize the price series with moving averages and a separate MACD panel for easier verification of the conversion.

## Conversion Mapping
| MT5 element | StockSharp equivalent |
| --- | --- |
| `iMA` on low/close | `WeightedMovingAverage` (low feed) and `ExponentialMovingAverage` (close feed) |
| `iMACD` main line | `MovingAverageConvergenceDivergence` main output |
| Position checks (`buy`, `sell`) | `Position` sign and volume handling via `BuyMarket` / `SellMarket` |
| Magic number & slippage | Not required in StockSharp high-level API |
| Stop-loss / Take-profit (pips) | `StartProtection` with absolute price offsets computed from pip size |

The resulting behaviour mirrors the MT5 version while leveraging StockSharp's strategy lifecycle, indicator binding, and risk management helpers.
