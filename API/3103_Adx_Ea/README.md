# 3103 — ADX EA (C#)

## Overview
The original MetaTrader "ADX EA" combines Average Directional Index breakouts with +DI/−DI crossovers, higher timeframe momentum confirmation, and a monthly MACD filter. The C# port replicates that multi-filter workflow on top of the StockSharp high-level API. The strategy subscribes to three candle streams:

1. **Primary timeframe** (default 5 minutes) — drives ADX, linear weighted moving averages, price structure checks, and volume filters.
2. **Momentum timeframe** (default 15 minutes) — produces the momentum deviations around the 100 baseline that gate entries.
3. **MACD timeframe** (default 30 days) — mirrors the monthly MACD that controls position exits.

## Trading logic
- **Breakout module** – When enabled, long trades require:
  - ADX or +DI above `EntryLevel` and the gap between +DI and −DI greater than `MinDirectionalDifference`.
  - The fast LWMA above the slow LWMA, bullish candle structure (`Low[2] < High[1]`), and growing momentum (`Momentum[1] > Momentum[2]`).
  - At least one of the last three momentum readings on the higher timeframe to deviate from 100 by more than `MomentumBuyThreshold`.
  - Rising volume on the primary timeframe (`Volume[1] > Volume[2]` or `Volume[1] > Volume[3]`).
  - MACD on the monthly timeframe bullish (`MacdMain[1] > MacdSignal[1]`).
  - ADX above `ExitLevel` to confirm overall trend strength.
  
  Short breakouts apply the symmetrical logic with −DI dominance, bearish structure (`Low[1] < High[2]`), momentum below 100 by `MomentumSellThreshold`, and a bearish MACD comparison.

- **Crossover module** – When active, looks for +DI crossing above −DI (longs) or −DI crossing above +DI (shorts). Optional filters mirror the original EA:
  - `RequireAdxSlope` demands ADX to be higher than the previous reading.
  - `ConfirmCrossOnBreakout` adds the same breakout threshold checks on the crossing bar.
  - `MinAdxMainLine` enforces a minimum ADX strength during the cross.
  - LWMA alignment, momentum slope, volume expansion, and MACD polarity must still agree with the intended direction.

- **Pyramiding** – Every new order adds volume according to `LotExponent`. The strategy treats `TradeVolume` as the base lot size and increases it by `LotExponent^n`, where `n` is the number of already opened steps. `MaxTrades` limits the amount of net volume that can be accumulated.

## Risk management
- **Protective orders** – `TakeProfitSteps` and `StopLossSteps` are fed to `StartProtection` and expressed in security price steps.
- **Trailing stop** – `TrailingStopSteps` maintains a manual trailing barrier beyond the best close price.
- **Break-even** – When `UseBreakEven` is enabled, the stop is tightened after price advances by `BreakEvenTrigger` steps and can offset the stop by `BreakEvenOffset` steps.
- **MACD exit** – When `EnableMacdExit` is true, the monthly MACD relationship closes longs when MACD falls below its signal (and vice versa for shorts), matching the `Close_BUY`/`Close_SELL` routines from the EA.
- **Equity stop** – `UseEquityStop` tracks the floating profit curve and liquidates positions once the drawdown reaches `TotalEquityRisk` percent.

Features that relied on account currency targets ("Take Profit in Money", "Trailing Profit in Money", etc.) are not ported because StockSharp strategies typically manage protective logic through stop distances and the built-in protection service. All other decision points from the EA are preserved with indicator equivalents.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeVolume` | 0.01 | Base lot size for the first entry. |
| `CandleType` | 5m timeframe | Primary candle series for ADX/LWMA logic. |
| `MomentumCandleType` | 15m timeframe | Higher timeframe for the momentum deviation filter. |
| `MacdCandleType` | 30-day timeframe | Timeframe that feeds the MACD exit filter. |
| `FastMaPeriod` | 6 | Fast linear weighted moving average length. |
| `SlowMaPeriod` | 85 | Slow linear weighted moving average length. |
| `AdxPeriod` | 14 | Average Directional Index period. |
| `MomentumPeriod` | 14 | Momentum indicator period on the higher timeframe. |
| `MacdFastPeriod` | 12 | Fast EMA period inside the MACD exit filter. |
| `MacdSlowPeriod` | 26 | Slow EMA period inside the MACD exit filter. |
| `MacdSignalPeriod` | 9 | Signal SMA period inside the MACD exit filter. |
| `EnableBreakoutStrategy` | true | Toggle for the ADX breakout branch. |
| `EnableCrossStrategy` | true | Toggle for the DI crossover branch. |
| `UseTrendFilter` | true | Enforces +DI dominance for longs and −DI dominance for shorts during breakouts. |
| `RequireAdxSlope` | true | Requires ADX to rise when evaluating DI crosses. |
| `ConfirmCrossOnBreakout` | true | Adds breakout thresholds to the crossover module. |
| `EnableMacdExit` | true | Enables the MACD-based exit routine. |
| `EntryLevel` | 10 | Minimum ADX/+DI/−DI level used by breakouts. |
| `ExitLevel` | 10 | Minimum ADX strength that allows new entries. |
| `MinDirectionalDifference` | 10 | Required gap between +DI and −DI. |
| `MinAdxMainLine` | 10 | Minimum ADX level during DI crosses. |
| `MomentumBuyThreshold` | 0.3 | Required deviation from 100 for bullish momentum confirmation. |
| `MomentumSellThreshold` | 0.3 | Required deviation from 100 for bearish momentum confirmation. |
| `MaxTrades` | 10 | Maximum number of pyramid steps. |
| `LotExponent` | 1.44 | Volume multiplier for each additional step. |
| `TakeProfitSteps` | 50 | Distance, in price steps, for the take-profit order. |
| `StopLossSteps` | 20 | Distance, in price steps, for the stop-loss order. |
| `TrailingStopSteps` | 40 | Manual trailing stop distance in price steps. |
| `UseBreakEven` | true | Activates the break-even relocation logic. |
| `BreakEvenTrigger` | 30 | Steps of favourable movement required before arming break-even. |
| `BreakEvenOffset` | 30 | Additional steps added to the entry price when moving the stop. |
| `UseEquityStop` | true | Enables the drawdown-based emergency exit. |
| `TotalEquityRisk` | 1 | Allowed percentage drawdown before flattening all positions. |

## Usage tips
- Align the `MomentumCandleType` and `MacdCandleType` with your primary timeframe to mimic the original timeframe mapping (e.g., 5-minute chart → 15-minute momentum, → monthly MACD).
- Tune `EntryLevel`, `MinDirectionalDifference`, and `MinAdxMainLine` together; lowering all three loosens the breakout filter considerably.
- `LotExponent` greater than 1.0 recreates the martingale-style scaling from the EA. Set it to 1.0 to keep position sizes constant.

