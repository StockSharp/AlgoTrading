# RSI Bollinger Bands EA (StockSharp Conversion)

## Overview
This strategy is a high-level StockSharp port of the MetaTrader 5 expert advisor "RSI Bollinger Bands EA". It trades on 15-minute candles and combines two independent RSI-based triggers:

* **Trigger One** – fixed overbought/oversold thresholds for RSI on M15, H1 and H4 together with a stochastic confirmation and slope filter.
* **Trigger Two** – adaptive RSI bands calculated from asymmetric standard deviations (separate positive/negative sigma) over configurable sample sizes on all three timeframes. The RSI must pierce the dynamic bands while the stochastic confirms momentum.

Both triggers require volatility contraction on the lower timeframe (M15 Bollinger spread), volatility expansion on the higher timeframe (H4 Bollinger spread) and a calm environment according to the H4 ATR. Only one trigger can be enabled at a time, mirroring the original EA restrictions.

## Data Requirements
* Primary execution candles: `M15CandleType` (default 15-minute). All entries and exits are evaluated on the close of these candles.
* Confirmation candles: `H1CandleType` (default 1-hour) for RSI conditions and statistics.
* Higher timeframe candles: `H4CandleType` (default 4-hour) for RSI checks, Bollinger spread filter and ATR volatility filter.

## Trading Logic
1. **Session filters**
   * Trading is limited to a configurable time window starting at `EntryHour` and lasting `OpenHours` hours. When `OpenHours` is zero the window lasts for the single opening hour.
   * Trading stops on Fridays once the candle hour reaches `FridayEndHour` (default 4, i.e. after 04:00 Friday).
   * A new position can only be opened when the current net position is flat (`Position == 0`).

2. **Volatility and spread filters (both triggers)**
   * The H4 Bollinger spread must be larger than `BbSpreadH4MinX` pips (X = 1 or 2) to ensure the higher timeframe range is wide enough.
   * The M15 Bollinger spread must stay below `BbSpreadM15MaxX` pips so that price is squeezed on the trading timeframe.
   * The H4 ATR converted to pips must remain below `AtrLimit`.

3. **Trigger One – fixed RSI levels**
   * RSI values for M15, H1 and H4 must fall below their respective "Low" thresholds to trigger a long setup, while remaining above "Low Limit" fail-safes.
   * RSI must rise relative to the previous M15 reading by more than `RDeltaM15Lim1` (default –3.5 pips) for longs, or fall by more than the mirrored threshold for shorts.
   * The stochastic main line must be below `StocLoM15_1` for longs or above `StocHiM15_1` for shorts.
   * Short entries require the RSI values to be above their "High" thresholds but remain below the "High Limit" fail-safes.

4. **Trigger Two – adaptive RSI sigma bands**
   * Historical RSI samples are kept for each timeframe (up to `NumRsi` elements). Separate positive and negative standard deviations are calculated to build asymmetric Bollinger-like bands.
   * Dynamic lower and upper bands for each timeframe are produced by applying `Rsi*M*Sigma2` multipliers (M15/H1/H4). Additional "limit" multipliers (`Rsi*M*SigmaLim2`) cap the allowed extremes.
   * Long entries require all three RSI values to be below their respective adaptive lower bands yet above the lower limits. The stochastic must be below `StocLoM15_2` and the RSI slope must be greater than `RDeltaM15Lim2`.
   * Short entries mirror the logic with upper bands and thresholds.

5. **Order execution and exits**
   * When a trigger fires, a market order of size `Volume` (default 0.1 lots) is placed.
   * Stop-loss and take-profit prices are derived from the configured pip distances for the active trigger (`StopLoss*X`, `TakeProfit*X`) using the instrument pip size heuristic (5-digit and 3-digit symbols receive 10x scaling).
   * Protective exits are simulated on the next M15 candle: if the candle high/low touches the stop or take profit level, the strategy closes the position at market and resets the protective prices. This mimics the MT5 behaviour that relied on stop orders.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Volume` | Trade volume in lots. | `0.1` |
| `TriggerOne` | Enable the fixed RSI trigger. | `true` |
| `TriggerTwo` | Enable the adaptive RSI trigger (mutually exclusive with trigger one). | `false` |
| `BbSpreadH4Min1` | Minimum H4 Bollinger spread (pips) for trigger one. | `84` |
| `BbSpreadM15Max1` | Maximum M15 Bollinger spread (pips) for trigger one. | `64` |
| `RsiPeriod1` | RSI length used by trigger one on all timeframes. | `10` |
| `RsiLoM15_1`, `RsiHiM15_1` | RSI thresholds for M15. | `24`, `66` |
| `RsiLoH1_1`, `RsiHiH1_1` | RSI thresholds for H1. | `34`, `54` |
| `RsiLoH4_1`, `RsiHiH4_1` | RSI thresholds for H4. | `48`, `56` |
| `RsiLoLim*`, `RsiHiLim*` | Safety limits to block extreme RSI readings. | `20–92` |
| `RDeltaM15Lim1` | Minimum RSI slope on M15 for trigger one. | `-3.5` |
| `StocLoM15_1`, `StocHiM15_1` | Stochastic bounds for trigger one. | `26`, `64` |
| `BbSpreadH4Min2` | Minimum H4 Bollinger spread (pips) for trigger two. | `65` |
| `BbSpreadM15Max2` | Maximum M15 Bollinger spread (pips) for trigger two. | `75` |
| `RsiPeriod2` | RSI length used by trigger two. | `20` |
| `NumRsi` | Sample size for RSI statistics. | `60` |
| `Rsi*M*Sigma2` | Multipliers for main adaptive bands (M15/H1/H4). | `1.20 / 0.95 / 0.9` |
| `Rsi*M*SigmaLim2` | Multipliers for outer limits (M15/H1/H4). | `1.85 / 2.55 / 2.7` |
| `RDeltaM15Lim2` | Minimum RSI slope on M15 for trigger two. | `-5.5` |
| `StocLoM15_2`, `StocHiM15_2` | Stochastic bounds for trigger two. | `24`, `68` |
| `TakeProfitBuy1`, `StopLossBuy1` | Protective distances in pips for trigger-one longs. | `150`, `70` |
| `TakeProfitSell1`, `StopLossSell1` | Protective distances in pips for trigger-one shorts. | `70`, `35` |
| `TakeProfitBuy2`, `StopLossBuy2` | Protective distances in pips for trigger-two longs. | `140`, `35` |
| `TakeProfitSell2`, `StopLossSell2` | Protective distances in pips for trigger-two shorts. | `60`, `30` |
| `AtrPeriod` | H4 ATR calculation period. | `60` |
| `BollingerPeriod` | Bollinger Bands length on M15 and H4. | `20` |
| `AtrLimit` | Maximum ATR in pips to allow entries. | `90` |
| `EntryHour` | Session start hour (0–23). | `0` |
| `OpenHours` | Session length in hours (0 = one hour). | `14` |
| `NumPositions` | Maximum simultaneous net positions (strategy opens only when flat). | `1` |
| `FridayEndHour` | Hour of Friday after which trading stops. | `4` |
| `StochasticK`, `StochasticD`, `StochasticSlowing` | Parameters for the stochastic oscillator. | `12 / 5 / 5` |
| `M15CandleType`, `H1CandleType`, `H4CandleType` | Candle data types for each timeframe. | `15m / 1h / 4h` |

## Notes
* The protective stop-loss and take-profit orders from the original EA are emulated by monitoring the M15 candle highs/lows. If intra-bar tick precision is required, consider adding stop orders via the low-level API.
* Ensure that the portfolio provides access to all requested timeframes; otherwise, the trigger queues will not form and the strategy will remain idle.
* The pip size heuristic follows the common MetaTrader convention: 5-digit (or 3-digit for JPY crosses) symbols multiply the exchange `PriceStep` by 10.
