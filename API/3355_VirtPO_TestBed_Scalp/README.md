# VirtPO TestBed Scalp Strategy

This strategy ports the **VirtPOTestBed_ScalpM1** MetaTrader 4 expert advisor to the StockSharp high level API. It keeps the original idea of creating *virtual pending orders* that are armed by Stochastic oscillator crossovers and executed once price momentum confirms the move. All filters, money-management rules and scheduling controls from the MQL version were replicated with StockSharp indicators and order methods.

## Core Logic

1. **Virtual pending orders** – When no position is open, the strategy checks the filter block on every completed candle:
   * Spread must remain below `SpreadMaxPips` (best bid/ask retrieved from Level1).
   * Average tick volume over the last three bars must exceed `VolumeLimit`.
   * Absolute price volatility (average body size for `VolatilityPeriod` bars) must be above `VolatilityLimit`.
   * Bollinger band width (period `BollingerPeriod`, width 2) has to stay between `BollingerLowerLimit` and `BollingerUpperLimit`.
   * Trading time must be inside the configured window (`EntryHour` + `OpenHours`) and outside disabled weekdays (`Day1`, `Day2`, Friday cut-off).
   * SMA trend filter – the difference between the fast (`SmaFastPeriod`) and slow (`SmaSlowPeriod`) SMA in pips must exceed `SmaDifferencePips` in either direction.
   * The body of the previous bar must be smaller than `LastBarLimitPips` to avoid chasing long candles.

   If the filters succeed, Stochastic crossovers are evaluated:
   * A bullish crossover through `StochasticSetLevel` arms a **virtual buy stop** above the bid by `PoThresholdPips`.
   * A bearish crossover through `100 - StochasticSetLevel` arms a **virtual sell stop** below the bid by the same threshold.
   Each virtual pending order remembers its expiry (`PoTimeLimitMinutes`) and the stop-loss / take-profit distances taken from `StopLossPips` and `TakeProfitPips`.

2. **Execution phase** – When `TickLevel` is enabled the strategy listens to incoming trades to execute virtual orders as soon as the last price breaches the threshold. If `TickLevel` is disabled the trigger check runs on the close of every finished candle. Once price crosses the virtual stop, a market order is sent and the virtual order is cleared.

3. **Risk management** – After a fill the strategy tracks:
   * Initial stop-loss and take-profit levels measured in pips from the entry price.
   * Optional trailing stop (`TrailingStopPips`) that follows the extreme price since entry.
   * Maximum holding time (`CloseTimeMinutes`). Depending on `ProfitType` it can close all positions (0), only profitable ones (1) or only losing ones (2) when the timer expires.

All price distances are converted from pips using the security `PriceStep` and digit multiplier, reproducing the five-digit broker handling in the MQL implementation. The default `OrderVolume` is applied to every market order. The strategy automatically resets its internal state when positions flatten.

## Important Notes

* Level1 data is required to compute spreads and trigger levels accurately. Without best bid/ask updates the filters will block trading.
* Tick-level execution mirrors the original EA’s `TickLevel` flag; when disabled, execution waits for candle closes which is more conservative but easier to back-test.
* The strategy only maintains a single net position just like the MQL version that restricted the number of active market orders.

## Parameters

| Group | Name | Description |
| --- | --- | --- |
| General | Candle Type | Time-frame used for candle subscription (default: 1 minute). |
| Execution | Tick Level | Use trade ticks to execute virtual orders immediately. |
| Execution | PO Threshold (pips) | Distance in pips between the bid price and the virtual stop level. |
| Execution | PO Lifetime (min) | Expiration time for each virtual pending order. |
| Filters | Max Spread (pips) | Maximum spread allowed before arming orders. |
| Filters | Volume Limit | Minimum average tick volume over the last three bars. |
| Filters | Volatility Period | Number of bars used to average absolute candle bodies. |
| Filters | Volatility Limit | Minimum average candle body size (in pips). |
| Filters | Bollinger Period | Bollinger band calculation period. |
| Filters | Bollinger Lower / Upper | Allowed band width range in pips. |
| Filters | Last Bar Limit | Maximum body size of the previous candle in pips. |
| Trend | Fast SMA / Slow SMA | Periods for the moving average trend filter. |
| Trend | SMA Difference | Minimal SMA distance in pips to confirm a trend. |
| Stochastic | %K / %D / Smooth | Standard Stochastic oscillator periods. |
| Stochastic | Stochastic Set | Level used to arm virtual pending orders. |
| Stochastic | Stochastic Go | Threshold used to execute the armed order. |
| Trading | Order Volume | Base market order volume. |
| Risk | Take Profit / Stop Loss / Trailing Stop | Exit distances in pips. |
| Schedule | Disable Days, First/Second No Trade Day | Weekday filters (use 99 to disable). |
| Schedule | Entry Hour / Open Hours | Trading window start and duration. |
| Schedule | Friday Cut-off | Hour after which Friday trading stops. |
| Risk | Max Lifetime | Time-based exit in minutes (set ≥5000 to disable). |
| Risk | Profit Filter | 0 – close regardless, 1 – close only winners, 2 – close only losers when the timer fires. |

## Differences From the Original EA

* The MQL `CPO` helper class is replaced with internal state variables that call `BuyMarket` / `SellMarket` directly once price crosses the virtual level.
* Stop-loss and take-profit execution uses candle highs/lows (for back-tests) or tick updates when available. Partial fills or hedged positions from the original MT4 environment are not supported.
* Account-based money management (`GLots`) is not ported; the StockSharp strategy uses the fixed `OrderVolume` parameter.

These adaptations preserve the trading idea while fitting StockSharp’s single-position, high-level programming model.
