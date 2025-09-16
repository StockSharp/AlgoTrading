# TDSGlobal 4

## Overview
TDSGlobal 4 is a conversion of the MetaTrader 4 expert advisor "TDSGlobal 4". The original system applies Alexander Elder's triple
screen method by combining the slope of a daily MACD histogram (OsMA) with a Williams %R filter. Orders are only deployed when th
e daily momentum aligns with the oscillator extremes, after which the strategy brackets the previous day's range with pending sto
p orders. The StockSharp port keeps the same breakout logic, adds precise scheduling so that different FX symbols fire at stagge
red minutes, and manages open exposure with optional trailing stops plus configurable take-profit targets.

## Strategy logic
### Higher timeframe filters
* **MACD slope** – compares the last two completed daily MACD main values (fast EMA 12, slow EMA 26, signal EMA 9). The bias is b
ullish when the most recent value exceeds the prior one, bearish when it is lower, and neutral when they are equal.
* **Williams %R** – evaluates the daily Williams %R (period 24). Long setups are allowed only when the reading is above the upper
 threshold (default −25, meaning overbought strength), while short setups require the value to stay below the lower threshold (de
fault −75).

### Breakout placement
* **Price levels** – on every finished daily candle the strategy records the previous day's high and low. New stop orders are pos
itioned one pip beyond those extremes (configurable via *EntryBufferPips*), mimicking the original EA's ±1 point offset.
* **Distance guard** – before sending a stop order the code enforces a minimum gap between the current best quote and the entry pr
ice (default 16 pips, matching the EA's 16 *Point* check). This prevents pending orders from being dropped too close to the marke
t when volatility is low.
* **Directional gating** – buy stops are created only when the MACD slope is positive and the Williams %R confirms bullish bias. S
ell stops require a negative slope and a Williams %R that indicates bearish pressure.

### Pending order maintenance
* **Daily reset** – when a new daily candle closes all leftover pending orders are cancelled so that the next trading session star
ts with a clean slate. If the filters do not allow a trade, no orders are placed for that day.
* **One trade per day** – once orders have been evaluated for a given day (whether they were placed or skipped), the strategy wait
s for the next daily close before reassessing. Filled stop orders automatically cancel the opposite side to avoid simultaneous lo
ng/short exposure.

### Risk management
* **Protective stops** – long positions inherit a protective exit just below the previous day's low, while short positions use the
 previous high. These levels are monitored on the one-minute trigger stream.
* **Take profit** – optional fixed targets expressed in pips relative to the actual fill price. Set *TakeProfitPips* to `0` to dis
able the target, mirroring the MT4 setting.
* **Trailing stop** – if *TrailingStopPips* is greater than zero the strategy reads best bid/ask quotes from Level1 data and trail
s the stop once price has moved in the trade's favor. When the trailing level is breached the position is closed at market.

### Scheduling
* **Minute windows** – to avoid simultaneous submissions across different currency pairs the EA used symbol-specific minute windo
ws. The port replicates this behavior: USDCHF uses minutes 0/8/16/24/32/40/48, GBPUSD 2/10/18/26/34/42/50, USDJPY 4/12/20/28/36/44
/52, and EURUSD 6/14/22/30/38/46/54. Any other instrument falls back to the entire hour (0–59).
* **Trigger stream** – a one-minute candle subscription drives both the scheduling of the daily orders and the intraday stop/take-
profit checks. The actual signal evaluation only occurs once per trading day during the first eligible minute.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Volume` | Order volume for stop entries. | `1` |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD configuration used to measure the daily slope. | `12 / 26 / 9` |
| `WilliamsPeriod` | Lookback for the Williams %R filter. | `24` |
| `WilliamsBuyLevel` | Upper threshold (typically −25) required before long orders are enabled. | `-25` |
| `WilliamsSellLevel` | Lower threshold (typically −75) required before short orders are enabled. | `-75` |
| `TakeProfitPips` | Take-profit distance in pips; `0` disables the target. | `999` |
| `TrailingStopPips` | Trailing stop distance in pips; `0` disables trailing. | `10` |
| `EntryBufferPips` | Offset added beyond the previous day's high/low before placing a stop order. | `1` |
| `MinDistancePips` | Minimum pip distance from the current quote to the pending order. | `16` |
| `DailyCandleType` | Timeframe that feeds the MACD and Williams %R filters. | `1 day` candles |
| `TriggerCandleType` | Lower timeframe used to schedule and monitor orders. | `1 minute` candles |

## Additional notes
* The C# implementation relies entirely on high-level StockSharp helpers (`SubscribeCandles`, `BuyStop`, `SellStop`, Level1 bindi
ng) so it can be reused inside the platform without manual order plumbing.
* Level1 data is required for trailing stop operation because the algorithm uses the best bid/ask quotes to move and trigger the
virtual stop.
* The package does not include a Python translation; only the C# strategy and multilingual documentation are provided.
