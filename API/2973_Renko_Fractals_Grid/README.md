# Renko Fractals Grid Strategy

## Overview
Renko Fractals Grid is a direct port of the MetaTrader 4 expert advisor "RENKO FRACTALS GRID". The strategy trades breakouts of recent Bill Williams fractals that are confirmed by a Renko-style volatility filter, a weighted moving average trend bias, and momentum strength derived from the rate of change indicator. The StockSharp version keeps the grid-style position management of the original robot, including martingale position sizing, break-even handling, trailing stops, equity protection, and optional floating profit trailing in currency units.

## Trading Logic
- **Fractal breakout:** A long setup requires the most recent bullish fractal to be broken by the last closed candle while at least one of the prior three closes remained below that level. Short trades mirror this behaviour with bearish fractals.
- **Renko filter:** The strategy inspects the high/low range of the last _CandlesToRetrace_ bars. A breakout is valid only when the current close is at least one Renko "box" (either a fixed pip distance or the latest ATR value) away from those extremes.
- **Trend filter:** Fast and slow weighted moving averages must be aligned (fast above slow for longs and below for shorts).
- **Momentum check:** The absolute deviation of the last three rate-of-change values from 100 must exceed the configured thresholds. This mimics the MQL momentum filter based on `iMomentum`.
- **MACD confirmation:** Trades are allowed only when the MACD main line is on the correct side of its signal line. The same check is used for exit timing.

## Risk Management
- **Martingale grid:** Every additional position multiplies the base volume by _LotExponent_ while the number of simultaneous trades is capped by _MaxTrades_.
- **Stop-loss and take-profit:** Static price offsets in pips are applied from the average entry price.
- **Break-even:** When the price advances by _BreakEvenTriggerPips_ the stop moves to entry plus _BreakEvenOffsetPips_.
- **Trailing stop:** A candle-based trailing stop maintains the best excursion observed since entry.
- **Money trailing:** Optional floating profit management closes all trades after a pullback of _MoneyStopLoss_ once the open profit exceeds _MoneyTakeProfit_.
- **Equity stop:** The strategy tracks the running equity peak (based on the portfolio value and open PnL). If the drawdown exceeds _EquityRiskPercent_ the entire position is liquidated.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Primary candle type used for all indicators. |
| `FastMaLength` / `SlowMaLength` | Periods of the weighted moving averages that define trend direction. |
| `MomentumLength` | Rate-of-change lookback for the momentum filter. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Minimum absolute deviation from 100 required for entries. |
| `UseAtrFilter` | Use ATR instead of a fixed pip distance for the Renko confirmation. |
| `BoxSizePips` | Size of the synthetic Renko box when ATR filtering is disabled. |
| `CandlesToRetrace` | Number of candles inspected when measuring recent highs and lows. |
| `BaseVolume` | Initial trade volume before applying the martingale multiplier. |
| `LotExponent` | Multiplier applied to each new position in the grid. |
| `MaxTrades` | Maximum number of concurrent positions per direction. |
| `StopLossPips` / `TakeProfitPips` | Static protective stop and target distances. |
| `TrailingStopPips` | Trailing stop distance in pips (set to zero to disable). |
| `UseBreakEven` | Enable moving the stop to break even. |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Distance required before break-even activation and the offset applied afterwards. |
| `UseMoneyTarget` | Enable floating profit trailing in currency units. |
| `MoneyTakeProfit` / `MoneyStopLoss` | Profit threshold that activates money trailing and the maximum permitted pullback. |
| `UseEquityStop` | Enable equity based global stop-out. |
| `EquityRiskPercent` | Maximum allowed drawdown from the equity peak before closing all trades. |

## Implementation Notes
- The original EA evaluates MACD on the monthly timeframe. The StockSharp port uses the same indicator configuration on the working timeframe because multi-timeframe data is not available by default.
- All price offsets that originated from "pips" in MQL are converted through the instrument's price step in order to work with fractional pip quotations.
- Realised profit tracking is approximated via filled order events, which is sufficient for equity-drawdown logic in the absence of broker-provided account statistics.
- The strategy uses high-level candle subscriptions with indicator binding as required by the project guidelines and keeps every inline comment in English as requested.
