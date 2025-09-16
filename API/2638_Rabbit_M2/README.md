# Rabbit M2 Strategy

## Overview
Rabbit M2 is a trend-following strategy that combines momentum oscillators, Donchian breakouts and adaptive position sizing. The original MetaTrader 5 version by Peter Byrom toggles between buying and selling regimes based on higher timeframe exponential moving averages (EMAs). Within the active regime the strategy waits for Williams %R swings confirmed by the Commodity Channel Index (CCI) before opening a trade. Positions are protected with fixed-distance stop loss and take profit targets and are force-closed when price violates the opposite Donchian channel boundary. After each profitable exit above a configurable profit target the strategy increases its base order size and doubles the profit target threshold, mimicking the scaling logic from the MQL expert advisor.

## Indicators and market data
- **Fast EMA (40) & Slow EMA (80)** calculated on 1-hour candles steer the trading direction and close trades on regime flips.
- **Commodity Channel Index (14)** measured on the primary timeframe confirms overbought or oversold momentum.
- **Williams %R (50)** on the primary timeframe provides the trigger when it crosses the -20/-80 levels.
- **Donchian Channel (100)** derived from the primary timeframe defines breakout exits when price breaches the previous 100-bar high or low.
- **Fixed stop loss and take profit** are set 50 pips away from the entry price (pip size adapts to 3/5-digit instruments).

Two data streams are required: the configurable primary timeframe for CCI/Williams %R/Donchian calculations and a dedicated 1-hour stream for the EMA trend filter.

## Trading rules
### Regime control
1. When the 40-period EMA on the H1 feed drops below the 80-period EMA, all long positions are closed and only short setups are allowed.
2. When the 40-period EMA rises above the 80-period EMA, all short positions are closed and only long setups are allowed.

### Entry conditions
- **Short entry**
  - Williams %R falls below -20 while its previous value was between -20 and 0.
  - CCI is above the sell level (default 101).
  - Short regime is active and the current net position volume is below the `MaxOpenPositions` limit.
- **Long entry**
  - Williams %R climbs above -80 while its previous value was between -100 and -80.
  - CCI is below the buy level (default 99).
  - Long regime is active and the current net position volume is below the `MaxOpenPositions` limit.

On every entry the strategy closes opposing exposure (if any) and opens the new position with the current base volume.

### Exit conditions
1. Stop loss and take profit are evaluated on every finished candle: longs exit if the low crosses the stop or the high reaches the target, shorts behave inversely.
2. Independent of stop/target, shorts exit when price closes above the previous 100-bar high and longs exit when price closes below the previous 100-bar low.
3. A regime flip (fast EMA crossing the slow EMA) immediately liquidates existing exposure.

### Position sizing logic
- The base order volume starts from `InitialVolume` (default 0.01) and follows exchange limits (step/min/max).
- After each realized profit larger than `BigWinTarget` the base volume increases by `VolumeStep` and the threshold doubles, preserving the cascading growth pattern of the original expert advisor.
- The `MaxOpenPositions` parameter limits net exposure. In the StockSharp port positions are netted, so hitting the limit means no additional volume is added until exposure drops.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CciSellLevel` | 101 | Minimum CCI value required to confirm a short setup. |
| `CciBuyLevel` | 99 | Maximum CCI value required to confirm a long setup. |
| `CciPeriod` | 14 | Period of the Commodity Channel Index on the primary timeframe. |
| `DonchianPeriod` | 100 | Lookback for the Donchian channel used in exit logic. |
| `MaxOpenPositions` | 1 | Maximum allowed net position multiples of the base volume. |
| `BigWinTarget` | 1.50 | Profit (in account currency) needed to scale the volume. |
| `VolumeStep` | 0.01 | Increment added to the base volume after a qualifying win. |
| `WprPeriod` | 50 | Length of the Williams %R oscillator. |
| `FastEmaPeriod` | 40 | Fast EMA period on the 1-hour trend feed. |
| `SlowEmaPeriod` | 80 | Slow EMA period on the 1-hour trend feed. |
| `TakeProfitPips` | 50 | Distance of the take profit in pips. |
| `StopLossPips` | 50 | Distance of the stop loss in pips. |
| `InitialVolume` | 0.01 | Starting order volume before scaling rules. |
| `CandleType` | 15-minute candles | Primary timeframe used for CCI/Williams %R/Donchian calculations. |

## Implementation notes
- The StockSharp port emulates MT5 stop loss and take profit by monitoring candle highs/lows instead of placing broker-attached orders.
- Price steps and pip calculations automatically adjust for 3 or 5 decimal instruments by multiplying the reported tick size by 10.
- The strategy relies on realized PnL updates to detect "big wins"; ensure trades are reported back to the strategy for scaling to work.
