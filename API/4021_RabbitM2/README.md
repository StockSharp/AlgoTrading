# Rabbit M2 Strategy

## Overview
Rabbit M2 is a discretionary expert advisor originally coded by Peter Byrom for MetaTrader 4. The algorithm alternates between
bullish and bearish regimes determined by hourly exponential moving averages. Within the active regime it listens for Williams %R
momentum swings that are confirmed by the Commodity Channel Index before submitting market orders. Protective logic mirrors the
source EA by attaching fixed-distance stop loss and take profit levels and by closing positions whenever price violates the
opposite Donchian channel boundary. A simple money-management module increases the base lot size after each highly profitable
trade and doubles the profit target required for the next scale-up.

## Market data and indicators
- **Primary timeframe** (default: 1-minute candles) supplies inputs for Williams %R, CCI and the Donchian channel.
- **Hourly timeframe** calculates the fast (40) and slow (80) EMA pair that controls the trading direction.
- **Williams %R (50)** acts as the momentum trigger when it crosses the -20/-80 bands.
- **Commodity Channel Index (14)** filters trades by requiring overbought or oversold readings.
- **Donchian Channel (100)** provides breakout exits based on the previous high/low range.
- **Static stop loss and take profit** are converted from point distances (default 50) into price offsets using the security tick
size, adjusted for 3 and 5 decimal instruments.

## Trading logic
### Regime management
1. When the 40-period EMA on the hourly feed drops below the 80-period EMA, every long position is closed and only short setups
are allowed.
2. When the 40-period EMA rises above the 80-period EMA, shorts are liquidated and the strategy permits only long trades.

### Entry rules
- **Short entries** require:
  - Williams %R to move from the -20..0 zone into oversold territory (< -20).
  - CCI to exceed the configurable sell threshold (default 101).
  - Net short exposure below the `MaxTrades` limit (each trade adds one base-volume unit).
- **Long entries** require:
  - Williams %R to climb out of the -100..-80 zone and print a value above -80.
  - CCI to fall beneath the buy threshold (default 99).
  - Net long exposure below the `MaxTrades` cap.

Each order is sent with the current base volume. The StockSharp port uses netting positions, so repeating signals simply increase
the net exposure until the configured limit is reached.

### Exit rules
1. Stop loss and take profit levels are monitored on every finished candle. Once the price crosses a level the position is
closed with a market order.
2. Independently of stop/target levels, a long position is closed when the close falls below the previous Donchian lower band;
a short is closed when the close rises above the previous Donchian upper band.
3. A regime flip caused by the hourly EMA crossover immediately liquidates positions that oppose the new direction.

### Money management
- The base order size starts from `InitialVolume` (default 0.01) and respects the security volume step, minimum and maximum.
- After each realized profit greater than `BigWinTarget` (default 15 currency units) the base volume increases by
`VolumeIncrement` (default 0.01) and the profit threshold doubles, matching the cascading behaviour of the MetaTrader version.
- When the strategy is flat, any pending stop/take placeholders are reset to avoid stale values.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CciSellLevel` | 101 | Minimum CCI value that confirms a short signal. |
| `CciBuyLevel` | 99 | Maximum CCI value that confirms a long signal. |
| `CciPeriod` | 14 | Commodity Channel Index lookback length. |
| `DonchianPeriod` | 100 | Donchian channel period used for breakout exits. |
| `MaxTrades` | 1 | Maximum number of base-volume units allowed in the net position. |
| `BigWinTarget` | 15 | Realized profit required before increasing the base volume. |
| `VolumeIncrement` | 0.01 | Additional volume added after a qualifying win. |
| `WprPeriod` | 50 | Williams %R calculation period. |
| `FastEmaPeriod` | 40 | Fast EMA period on the hourly trend feed. |
| `SlowEmaPeriod` | 80 | Slow EMA period on the hourly trend feed. |
| `TakeProfitPoints` | 50 | Take profit distance in price points. |
| `StopLossPoints` | 50 | Stop loss distance in price points. |
| `InitialVolume` | 0.01 | Starting base order size. |
| `CandleType` | 1-minute candles | Primary timeframe used for momentum and exit calculations. |

## Implementation notes
- Stop loss and take profit levels are evaluated inside the strategy rather than submitted as separate orders to replicate the
behaviour of MetaTrader's `OrderSend` parameters.
- Volume adjustments rely on realized PnL reported by StockSharp. Make sure the strategy receives trade confirmations from the
broker connection so that the scaling logic activates.
- The helper method `CalculatePriceOffset` inflates the point size for 3- and 5-decimal forex symbols, reproducing the `Point`
constant from the original platform.
