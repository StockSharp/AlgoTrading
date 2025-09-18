# Cryptocurrency Fibonacci MAs (StockSharp)

## Overview
This strategy ports the MetaTrader expert advisor "Cryptocurrency Fibonacci MAs" to StockSharp's high-level API. The system tracks a stack of Fibonacci-based exponential moving averages (8/13/21/55), validates momentum on a higher timeframe, and confirms the macro trend with a monthly MACD filter before sending market orders. Only completed candles are processed and all indicator updates are performed via the `Bind`/`BindEx` pipeline.

Compared to the MetaTrader version the following intentional adjustments were made:
- Money-based take profit, equity stop-out, candle-by-candle trailing and break-even automation were omitted. The StockSharp port uses classic pip-based stop-loss and take-profit via `StartProtection`.
- Order pyramiding is limited to a net position per direction. Reversals close the opposite exposure first, mirroring StockSharp's netted position model.
- Multi-timeframe data is provided through additional candle subscriptions rather than ad-hoc indicator requests on demand.

## Trading logic
### Long entry
1. EMA alignment: 8 > 13 > 21 > 55 on the main timeframe.
2. Higher timeframe momentum: the absolute deviation of the 14-period Momentum from the neutral 100 level is above the configured buy threshold for at least one of the last three higher timeframe candles.
3. Monthly MACD filter: MACD main line is above the signal line.
4. Position filter: current net position must be flat or short and remain below the configured maximum volume.

### Short entry
1. EMA alignment: 8 < 13 < 21 < 55.
2. Momentum deviation above the sell threshold for at least one of the last three higher timeframe candles.
3. MACD main line below its signal line.
4. Net exposure must be flat or long and within the `MaxPositions` limit.

### Exit logic
- `StartProtection` places protective stop-loss and take-profit orders expressed in pip distances. No additional trailing or break-even logic is applied in this port.
- Reversal signals send the opposite market order size, which first offsets the existing position before establishing the new exposure.

## Multi-timeframe mapping
The higher timeframe used for the momentum indicator mirrors the original coefficient table:

| Main timeframe | Momentum timeframe |
| --- | --- |
| 1 minute | 15 minutes |
| 5 minutes | 30 minutes |
| 15 minutes | 1 hour |
| 30 minutes | 4 hours |
| 1 hour | 1 day |
| 4 hours | 1 week |
| 1 day | 1 month |
| 1 week | 1 month |
| 1 month | 1 month |

The MACD confirmation always runs on a 30-day monthly approximation.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Order size in lots. | 0.1 |
| `StopLossPips` | Stop-loss distance in pips. | 20 |
| `TakeProfitPips` | Take-profit distance in pips. | 50 |
| `MomentumBuyThreshold` | Minimum absolute momentum deviation from 100 required for long trades. | 0.3 |
| `MomentumSellThreshold` | Minimum absolute momentum deviation from 100 required for short trades. | 0.3 |
| `MaxPositions` | Maximum net volume per direction expressed as multiples of `TradeVolume`. | 1 |
| `CandleType` | Primary timeframe for EMA calculations. | 1-hour candles |

## Usage notes
1. Attach the strategy to a symbol and select an appropriate timeframe through `CandleType`.
2. Ensure that the data source can provide both the main timeframe and the derived higher timeframes (momentum and monthly).
3. Adjust pip-based risk parameters to match the instrument's tick size. The helper converts pips to instrument steps using `Security.PriceStep`.
4. Backtesting and optimization can fine-tune the momentum thresholds and stop distances using the provided parameter ranges.
