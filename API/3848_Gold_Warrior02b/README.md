# GoldWarrior02b Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Comprehensive StockSharp port of the MetaTrader 4 expert advisor *GoldWarrior02b* (folder `MQL/7694`).
It blends a Commodity Channel Index (CCI), a custom impulse gauge and a handcrafted ZigZag swing detector
and evaluates signals only a few seconds before every 15 minute boundary. The goal of this translation is
to mimic the high-level logic of the original robot while respecting StockSharp's net-position execution model.

## Key Characteristics

- **Impulse filter** – replaces the `DayImpuls` custom indicator by averaging the candle open/close distance
  normalised by the instrument's price step.
- **ZigZag structure** – rebuilds recent swing highs and lows to determine whether the market is trending up or down.
- **Timing gate** – entries are allowed only when the current candle closes during the last 15 seconds of minutes 14, 29, 44 or 59.
- **Risk controls** – includes stop-loss, take-profit, trailing stop (optional) and an account-wide profit target measured
  in currency units. Defaults mirror the MetaTrader inputs (1,000 point stop, 150 point take-profit, trailing disabled).
- **Net exposure** – StockSharp keeps a single net position per security, so the multi-level hedging and lot scaling
  from the MQL implementation are not reproduced. Instead, the strategy focuses on a single entry volume.

## Trading Logic

### Signal Preparation

1. Subscribe to candles defined by `CandleType` (5 minute timeframe by default).
2. Calculate CCI and the impulse average using the shared `ImpulsePeriod` (default 21 bars).
3. Update the ZigZag swing direction once the deviation exceeds `ZigZagDeviation` points and the depth/backstep
   constraints are met.
4. Store the previous values of the indicators to replicate the "current" (`cci0`, `imp`) and "previous" (`cci1`, `nimp`)
   buffers used in the expert advisor.

### Entry Rules

A setup is evaluated only if no position is currently open, at least 15 seconds have passed since the last exit and
`AllowEntryTime` returns `true` (end of the 15 minute block).

**Long:**
- Latest ZigZag swing points downward (new low lower than the previous one).
- Either
  - current CCI increases compared to the previous bar, the previous CCI is below -50, the current CCI stays below -30,
    the impulse turns positive and the previous impulse was negative; or
  - current CCI is below -200, the previous CCI was still lower, the impulse remains below `ImpulseBuyThreshold`
    and is stronger than the previous impulse.

**Short:**
- Latest ZigZag swing points upward (new high higher than the previous one).
- Either
  - current CCI decreases compared to the previous bar, the previous CCI is above 50, the current CCI stays above 30,
    the impulse turns negative and the previous impulse was positive; or
  - current CCI is above 200, the previous CCI was higher, the impulse stays above `ImpulseSellThreshold`
    and is weaker than the previous impulse.

If the previous impulse value lies between `ImpulseSellThreshold` and `ImpulseBuyThreshold` the signal is ignored.

### Exit Management

- **Stop-loss** – triggers when price moves `StopLossPoints` beyond the entry price (1,000 points by default).
- **Take-profit** – closes the position after travelling `TakeProfitPoints` (150 points).
- **Trailing stop** – optional; when enabled it activates after price moves `TrailingStopPoints + TrailingStepPoints`
  in favour of the position and then trails price by `TrailingStopPoints`.
- **Profit target** – converts the open PnL into account currency using `PriceStep` and `StepPrice` and
  closes the position once it exceeds `ProfitTarget` (default 300).

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `BaseVolume` | Trade size for entries. | `0.1` |
| `StopLossPoints` | Stop distance in points. | `1000` |
| `TakeProfitPoints` | Take-profit distance in points. | `150` |
| `TrailingStopPoints` | Trailing stop distance in points (0 disables trailing). | `0` |
| `TrailingStepPoints` | Additional distance before trailing activates. | `0` |
| `ImpulsePeriod` | Period for both CCI and impulse calculations. | `21` |
| `ZigZagDepth` | Minimum bars between new ZigZag swings. | `12` |
| `ZigZagDeviation` | Minimum price move (in points) to confirm a swing. | `5` |
| `ZigZagBackstep` | Minimum bars before accepting a new swing. | `3` |
| `ProfitTarget` | Unrealised profit threshold (account currency). | `300` |
| `ImpulseSellThreshold` | Minimum impulse value required for shorts. | `-30` |
| `ImpulseBuyThreshold` | Maximum impulse value allowed for longs. | `30` |
| `CandleType` | Working timeframe. | `5 minute time frame` |

## Differences vs. Original Expert Advisor

- MetaTrader version uses `GlobalVariableSet` to rate-limit orders and stores ticket counts for hedging grids.
  This port retains the time-based throttle but not the averaging/hedging ladder because StockSharp accounts
  are netted.
- Order management is handled via market orders (`BuyMarket`, `SellMarket`) to stay within the high-level API guidance.
- The impulse calculation is simplified; the original `DayImpuls` exposes two buffers (`imp`, `nimp`). Here both buffers
  are approximated by the current and previous moving average readings.

## Usage Tips

- Configure `CandleType` to match the timeframe used during optimisation (the original EA works on M5).
- Ensure the instrument provides `PriceStep` and `StepPrice` metadata to convert point distances correctly.
- Back-test with realistic slippage/latency to confirm the entry gate (last seconds before the quarter-hour) behaves as expected.

## Disclaimer

This strategy is supplied for educational purposes. Thoroughly test with historical and forward data before
risking real capital.
