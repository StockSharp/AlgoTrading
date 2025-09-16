# Currency Strength v1.1 Strategy

## Overview
The Currency Strength v1.1 strategy replicates the MetaTrader expert advisor *Currency Strength v1.1*. It measures the relative strength of the eight major currencies (USD, EUR, JPY, CAD, AUD, NZD, GBP, CHF) using daily percentage changes for 26 liquid FX pairs. Whenever the strength of two currencies diverges beyond a configurable threshold, the strategy opens a position in the corresponding currency pair in the direction of the stronger currency.

## Market and data
- **Instrument universe:** 26 major and cross FX pairs (USDJPY, USDCAD, AUDUSD, USDCHF, GBPUSD, EURUSD, NZDUSD, EURJPY, EURCAD, EURGBP, EURCHF, EURAUD, EURNZD, AUDNZD, AUDCAD, AUDCHF, AUDJPY, CHFJPY, GBPCHF, GBPAUD, GBPCAD, GBPJPY, CADJPY, NZDJPY, GBPNZD, CADCHF).
- **Data frequency:** Daily candles (D1). Only completed candles are processed to maintain consistent calculations.
- **Required fields:** Open, high, low, close prices from each candle.

## Currency strength calculation
The daily percentage change for every pair is computed as:

```
(change) = (Close − Open) / Open × 100
```

These pair-specific changes are then combined into currency strength indices:

- **EUR strength** = average of EURJPY, EURCAD, EURGBP, EURCHF, EURAUD, EURUSD, EURNZD
- **USD strength** = average of USDJPY, USDCAD, –AUDUSD, USDCHF, –GBPUSD, –EURUSD, –NZDUSD
- **JPY strength** = negative average of USDJPY, EURJPY, AUDJPY, CHFJPY, GBPJPY, CADJPY, NZDJPY
- **CAD strength** = average of CADCHF, CADJPY, –GBPCAD, –AUDCAD, –EURCAD, –USDCAD
- **AUD strength** = average of AUDUSD, AUDNZD, AUDCAD, AUDCHF, AUDJPY, –EURAUD, –GBPAUD
- **NZD strength** = average of NZDUSD, NZDJPY, –EURNZD, –AUDNZD, –GBPNZD
- **GBP strength** = average of GBPUSD, –EURGBP, GBPCHF, GBPAUD, GBPCAD, GBPJPY, GBPNZD
- **CHF strength** = average of CHFJPY, –USDCHF, –EURCHF, –AUDCHF, –GBPCHF, –CADCHF

Each average uses the same number of components as in the original expert advisor to preserve the weighting scheme.

## Trading logic
1. After all 26 pairs report a new finished daily candle, the strengths are recalculated.
2. For every pair the strategy compares the two relevant currency strengths. If the absolute difference exceeds the `DifferenceThreshold` parameter, a trade signal is generated.
3. The signal direction follows the stronger currency:
   - If base currency strength > quote currency strength → buy the pair.
   - If base currency strength < quote currency strength → sell the pair.
4. Trades are only allowed when the pair’s daily candle agrees with the signal (close above open for buys, close below open for sells), mirroring the original EA’s trend filter.
5. Existing net positions are respected. If a reversal signal appears while an opposite position is open, the strategy closes the current position and flips to the new direction with a single market order.
6. When `TradeOncePerDay` is enabled, each pair can enter long at most once per trading day and enter short at most once per trading day.

## Risk management and exits
- The optional `UseSlTp` flag enables stop-loss and take-profit logic executed on the daily candle of each pair. The distances are defined in pips (`StopLossPips`, `TakeProfitPips`).
- The protective logic evaluates the daily high/low of the most recent candle. If those extremes reach the respective targets, the position is closed at the market price on the next evaluation step.
- Without SL/TP, positions remain open until an opposite signal forces a reversal or the strategy is stopped manually, mirroring the source EA behaviour.

## Strategy parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Timeframe for candles (default: daily). |
| `DifferenceThreshold` | Minimum strength gap (in percentage points) required to trigger a trade. |
| `TradeOncePerDay` | If `true`, limits each pair to one long and one short entry per day. |
| `UseSlTp` | Enables daily evaluation of stop-loss and take-profit levels. |
| `TakeProfitPips` | Take-profit distance measured in pips. |
| `StopLossPips` | Stop-loss distance measured in pips. |
| Pair parameters | Individual `Security` inputs for the 26 FX pairs. Each must be assigned before starting the strategy. |
| `Volume` | Base class property defining the trade size (default 0.01 lots). |

## Implementation notes
- The strategy subscribes to each pair separately using the high-level candle subscription API (`SubscribeCandles`).
- Candle handling strictly ignores incomplete candles, satisfying the StockSharp conversion guidelines.
- Strength calculations and signal generation only run when all pairs report the same trading date, guaranteeing synchronized currency baskets.
- Internal dictionaries track last trade dates per direction and store entry information for protective exits.

## Usage tips
1. Assign all 26 securities before starting the strategy; missing inputs throw an exception to prevent partial calculations.
2. Ensure that the data provider supplies daily candles for every configured pair so that the currency strengths stay synchronized.
3. Adjust `DifferenceThreshold` to control signal frequency. Smaller thresholds lead to more frequent trades but also more reversals.
4. Calibrate the pip-based stops to the quoting precision of your broker; the default assumes fractional pip pricing.
