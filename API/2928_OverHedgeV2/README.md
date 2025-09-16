# OverHedge V2 Grid Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the MetaTrader OverHedge V2 expert advisor on the StockSharp high-level API. It builds a hedged grid by following the direction of a fast and a slow EMA, then alternates long and short orders inside a dynamic tunnel. Positions are added according to a geometric lot progression and the whole basket is liquidated once the aggregated unrealized profit reaches the configured target.

## Trading Logic

- **Trend filter:** An 8-period EMA must diverge from a 21-period EMA by at least `MinDistancePips`. The filter decides the direction of the first trade in each cycle.
- **Grid tunnel:** The tunnel width equals the current spread multiplied by two plus `TunnelWidthPips` converted to price units. It defines the opposite-side trigger once the cycle starts.
- **Order alternation:** The first three positions are opened in the trend direction. Afterwards the algorithm alternates side to hedge the exposure using the same tunnel anchors as the reference.
- **Lot escalation:** Each subsequent order multiplies the previous volume by `BaseMultiplier` starting from `StartVolume`. The size is aligned to instrument volume constraints.
- **Cycle exit:** When the net unrealized gain per instrument lot is above `MinProfitTargetPips` and the total basket profit exceeds `ProfitTargetPips`, the strategy closes all open positions and resets the state.
- **Manual shutdown:** Setting `ShutdownGrid` to `true` closes any remaining position and prevents new orders until it is toggled off.

## Entry Conditions

### Long entries
- Trend filter indicates an uptrend (`EMA_short - EMA_long > MinDistancePips`).
- Ask price is greater than or equal to the current buy anchor.
- The strategy is not in shutdown mode and the basket has not reached its profit target.

### Short entries
- Trend filter indicates a downtrend (`EMA_long - EMA_short > MinDistancePips`).
- Ask price is less than or equal to the current sell anchor.
- Shutdown flag is false and the basket profit target is not yet hit.

## Exit Management

- **Profit exit:** When the unrealized basket profit satisfies `ProfitTargetPips` with every open side gaining at least `MinProfitTargetPips` per lot, all positions are closed at market.
- **Emergency exit:** Setting `ShutdownGrid` to `true` immediately closes any open exposure.

## Indicators and Data

- 8-period EMA (fast) and 21-period EMA (slow) calculated on the configured candle series.
- Level 1 subscription is used to track best bid/ask in order to build the tunnel and compare entry conditions with real-time spreads.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `StartVolume` | Initial volume of the first order in a cycle. |
| `BaseMultiplier` | Geometric multiplier applied to the volume of each subsequent order. |
| `TunnelWidthPips` | Additional tunnel width in pips added to twice the current spread. |
| `ProfitTargetPips` | Basket profit target measured in pips converted to price distance. |
| `MinProfitTargetPips` | Minimum favorable move per side before the basket can close. |
| `ShortEmaPeriod` | Period of the fast EMA used for direction confirmation. |
| `LongEmaPeriod` | Period of the slow EMA used for direction confirmation. |
| `MinDistancePips` | Minimum EMA separation required to declare a trend. |
| `CandleType` | Time frame of the candles feeding the EMAs and the trading loop. |
| `ShutdownGrid` | Boolean switch that forces liquidation and blocks new trades. |

## Practical Notes

- The default candle period is one hour; adjust it to match the timeframe used in the original EA.
- The strategy relies on best bid/ask data; provide Level 1 quotes during live trading or backtesting.
- Because StockSharp maintains a net position per instrument, alternating buys and sells will reduce or flip the net exposure instead of holding independent hedged tickets, but the basket logic still mimics the intended profit capture.
- Always verify instrument-specific volume steps and tick sizes so the generated tunnel and lot scaling match the market you trade.
