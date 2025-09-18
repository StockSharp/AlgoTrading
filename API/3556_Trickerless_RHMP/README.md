# Trickerless RHMP Strategy (StockSharp Port)

This strategy ports the MetaTrader expert advisor **Trickerless RHMP** to StockSharp's high level API. It keeps the multi-stage
entry logic of the original robot – combining Average Directional Index confirmation, smoothed moving average structure and
volatility driven position management – while following the framework conventions documented in `AGENTS.md`.

## Trading Logic

1. **Indicators**
   - Average True Range (ATR) with configurable period for volatility sizing.
   - Average Directional Index (ADX) with full +DI/-DI components to qualify trend strength.
   - Two smoothed moving averages (SMMA) representing the fast and slow trend filters.

2. **Trend evaluation**
   - The slow SMMA slope must be within the `MinSlopePips`…`MaxSlopePips` corridor (measured in instrument pips).
   - ADX must exceed `AdxThreshold` and rise compared with the previous candle.
   - Price has to stay at least `TrendSpacePips` away from the fast SMMA to avoid congestion.
   - A bullish bias requires the fast SMMA above the slow SMMA, +DI ≥ -DI and a rising fast average. Bearish bias mirrors these
     checks.

3. **Primary entries**
   - When the bullish (or bearish) bias is active, the strategy opens a long (or short) order with volume `OrderVolume`, respecting
     `MaxNetPositions` and waiting at least `SleepInterval` between entries.
   - If an opposite net position exists it is flattened first to keep hedging disabled.

4. **Spike entries**
   - If the current candle range exceeds `CandleSpikeMultiplier` times the previous range, the strategy can fire an auxiliary
     position in the direction of the candle body when the ADX components agree. The position uses `OrderVolume * SpikeVolumeMultiplier`.

## Risk Management

- ATR-based stop-loss, take-profit and optional trailing-stop (`StopLossAtrMultiplier`, `TakeProfitAtrMultiplier`, `TrailingAtrMultiplier`).
- Session-wide protection: once realized PnL reaches `DailyProfitTarget` (fraction of starting equity), new entries are blocked.
- Global emergency switch `EmergencyExit` closes all positions immediately when toggled.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Timeframe used for all calculations. | 5-minute candles |
| `OrderVolume` | Base volume for each entry. | 0.03 |
| `AtrPeriod` | ATR lookback length. | 14 |
| `AdxPeriod` | ADX lookback length. | 14 |
| `AdxThreshold` | Minimum ADX value to enable trading. | 10 |
| `FastMaPeriod` | Fast smoothed moving average period. | 60 |
| `SlowMaPeriod` | Slow smoothed moving average period. | 120 |
| `MinSlopePips` / `MaxSlopePips` | Allowed slope corridor for the slow SMMA. | 2 / 9 |
| `TrendSpacePips` | Minimal price distance from the fast SMMA (in pips). | 5 |
| `CandleSpikeMultiplier` | How much larger the candle range must be to trigger spike entries. | 7 |
| `TakeProfitAtrMultiplier` | ATR multiples for take-profit. | 1.0 |
| `StopLossAtrMultiplier` | ATR multiples for stop-loss. | 1.5 |
| `TrailingAtrMultiplier` | ATR multiples for trailing-stop (0 disables). | 0 |
| `MaxNetPositions` | Maximum number of simultaneous net position units. | 1 |
| `SleepInterval` | Minimum time between consecutive entries. | 24 minutes |
| `DailyProfitTarget` | Fraction of initial equity that blocks trading once reached. | 0.045 |
| `AllowNewEntries` | Master switch to enable/disable entries. | true |
| `SpikeVolumeMultiplier` | Volume multiplier for spike entries. | 1.0 |
| `EmergencyExit` | Closes all positions immediately when true. | false |

## Notes

- The StockSharp port focuses on the clean high level API instead of the ticket-by-ticket micro-management from MetaTrader. All
  money management logic is implemented through `Volume` and ATR based levels.
- The original EA had several balance and margin checks. These are approximated with the `DailyProfitTarget`, `MaxNetPositions`
  and ATR sizing parameters so that the behaviour stays aligned without direct MT4 account calls.
- Because the strategy uses smoothed averages, make sure a sufficient warm-up period is present before evaluating trades.
