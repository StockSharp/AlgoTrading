# Yen Trader 05.1 Strategy (C#)

## Overview

The Yen Trader 05.1 strategy replicates the original MetaTrader expert advisor that arbitrages the relationship between three currency pairs:

- **Trading cross** – the instrument that hosts the strategy instance (for example GBPJPY).
- **Major pair** – typically the base currency of the cross against USD (for example GBPUSD).
- **USDJPY** – used to confirm the yen leg of the triangle.

A breakout on the major pair combined with confirmation from USDJPY generates the trading signals. Optional RSI, CCI, RVI and moving-average filters refine the entries. Position management supports both averaging and pyramiding, while risk management reproduces the pip/ATR based stop handling from the EA.

## Trading Logic

1. **Breakout detection**
   - `LoopBackBars` controls the lookback window. When it is greater than 1 the strategy checks either:
     - recent highs/lows (`PriceReference = HighLow`), or
     - closes from `LoopBackBars` bars ago (`PriceReference = Close`).
   - `MajorDirection` defines how the major and the yen legs should move relative to each other when the cross is quoted as major/yen (Left) or yen/major (Right).
2. **Entry filters**
   - `UseRsiFilter` requires RSI above/below 50 depending on the expected trend alignment.
   - `UseCciFilter` enforces CCI to be positive/negative.
   - `UseRviFilter` waits for RVI to cross its signal line. The signal line is a 4-period SMA of the RVI values, just like the MT4 implementation.
   - `UseMovingAverageFilter` keeps entries aligned with a configurable moving average (`MaMode`, `MaPeriod`).
3. **Entry style**
   - `EntryMode = Both` allows any breakout.
   - `EntryMode = Pyramiding` only adds on bullish/bearish candles in the trade direction.
   - `EntryMode = Averaging` only adds when the previous candle closed against the position to average down.
4. **Order sizing**
   - `FixedLotSize` places a constant volume.
   - When the fixed lot is zero the strategy uses `BalancePercentLotSize` and the current portfolio value to size trades.
   - `MaxOpenPositions` limits the cumulative size (number of additive entries).
5. **Risk management**
   - Pip distances (`StopLossPips`, `TakeProfitPips`, `BreakEvenPips`, `ProfitLockPips`, `TrailingStopPips`, `TrailingStepPips`) are translated via `Security.MinPriceStep`.
   - When `EnableAtrLevels` is active, ATR distances replace pips using the daily ATR (`AtrCandleType`, `AtrPeriod`) and the respective multipliers.
   - Stops, take-profits, break-even, profit lock and trailing levels are updated from completed candles, just like the MQL implementation.
   - `CloseOnOpposite` will flip existing positions instead of stacking new ones when an opposite breakout appears.
   - `AllowHedging` lets the strategy add to a position even if an opposite position is still open. Note that StockSharp strategies use net positions, so simultaneous long/short positions are not supported; the flag effectively controls whether the strategy is allowed to increase exposure when the current net position points the other way.

## Parameters

| Group | Name | Description |
|-------|------|-------------|
| Instruments | `MajorSecurity` | Major pair used for breakout confirmation. |
| | `UsdJpySecurity` | USDJPY security for the yen leg confirmation. |
| Data | `CandleType` | Signal timeframe for all three pairs. |
| Filters | `MajorDirection` | Alignment between the major pair and the traded cross (Left = major/yen, Right = yen/major). |
| | `PriceReference` | Either high/low breakout or delayed close comparison. |
| | `LoopBackBars` | Number of historical bars to evaluate the breakout. |
| | `EntryMode` | Averaging, pyramiding or both. |
| Indicators | `UseRsiFilter`, `UseCciFilter`, `UseRviFilter`, `UseMovingAverageFilter` | Enable/disable additional confirmation filters. |
| | `MaPeriod`, `MaMode` | Moving average configuration. |
| Risk | `FixedLotSize`, `BalancePercentLotSize` | Volume controls. |
| | `MaxOpenPositions` | Maximum number of additive entries. |
| | `StopLossPips`, `TakeProfitPips`, `BreakEvenPips`, `ProfitLockPips`, `TrailingStopPips`, `TrailingStepPips` | Pip-based risk distances. |
| | `EnableAtrLevels`, `AtrCandleType`, `AtrPeriod`, `AtrStopLossMultiplier`, `AtrTakeProfitMultiplier`, `AtrTrailingMultiplier`, `AtrBreakEvenMultiplier`, `AtrProfitLockMultiplier` | ATR-based risk configuration. |
| Behaviour | `CloseOnOpposite` | Close or flip positions on opposite signals. |
| | `AllowHedging` | Allow entries when an opposite net position exists. |

## Usage Notes

- Assign the traded cross security to the strategy `Security` property, then set `MajorSecurity` and `UsdJpySecurity` to the supporting instruments.
- Ensure the portfolio is connected; variable lot sizing requires `Portfolio.CurrentValue`.
- The strategy expects synchronized candle data for all three instruments. If different exchanges deliver data with different session calendars, consider resampling to a common timeframe.
- ATR calculations subscribe to the configured `AtrCandleType`. Keep it aligned with the original EA defaults (daily, 21 periods) for comparable behaviour.
- Risk logic operates on closed candles, so protective orders are executed by market exits when the thresholds are breached during the subsequent candle.

## Differences vs. MT4 Version

- StockSharp uses aggregated net positions; true hedging (holding long and short simultaneously) is not available. `AllowHedging` simply controls whether the strategy can flip positions automatically when a new signal appears.
- Stop/limit management is implemented with market exits after the thresholds are triggered on candle data. The original EA modifies order stops directly because it operates at tick level.
- The RVI signal line is implemented as a four-period SMA of the RVI values, matching the behaviour of `MODE_SIGNAL` in MT4.

