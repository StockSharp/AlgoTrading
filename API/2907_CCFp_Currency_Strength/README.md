# CCFp Currencies Strength Strategy

## Overview
This strategy ports the classic MetaTrader CCFp expert advisor into the StockSharp high-level API. It calculates a relative strength score for the eight major currencies (USD, EUR, GBP, CHF, JPY, AUD, CAD, NZD) using ratios between fast and slow simple moving averages on the seven USD-based majors (EURUSD, GBPUSD, AUDUSD, NZDUSD, USDCAD, USDCHF, USDJPY). When the difference between two currency strengths breaks above a configurable threshold, the strategy opens market positions that express the stronger currency against the weaker one.

The implementation follows the recommended high-level architecture: each instrument has its own candle subscription, indicators are bound via `Bind`, and order management uses `RegisterOrder` with market orders. Comments on executed orders reuse the original `(TOPDOWN)` format to keep the same bookkeeping style as the MQL version.

## Required instruments
Attach the following securities to the strategy parameters:

- `EURUSD`
- `GBPUSD`
- `AUDUSD`
- `NZDUSD`
- `USDCAD`
- `USDCHF`
- `USDJPY`

All seven pairs must share the same timeframe that is set through the `Candle Type` parameter.

## Parameters
| Parameter | Description |
| --- | --- |
| `Fast MA` | Fast moving average period used inside the strength calculation. |
| `Slow MA` | Slow moving average period used inside the strength calculation. |
| `Strength Step` | Minimal difference between two currencies that must be exceeded to trigger a new signal. |
| `Close Opposite` | If enabled the strategy closes opposite positions before submitting a new order. |
| `Candle Type` | Candle series processed by the indicators. |
| Base `Volume` | Taken from the standard `Strategy.Volume` property and used for every submitted market order. |

## Trading logic
1. Each of the seven USD majors is subscribed with its own pair of simple moving averages (fast and slow).
2. Every time a finished candle arrives the strategy converts the ratio of the slow and fast averages into the same synthetic strength values produced by the original CCFp indicator.
3. After all seven pairs are updated, the eight currency strength scores are recomputed.
4. When the difference between a “top” currency and a “down” currency crosses the `Strength Step` level upward, while the top currency is rising and the down currency is falling, an opportunity is detected.
5. The strategy opens market orders that express long exposure to the strong currency and short exposure to the weak currency:
   - If USD is the strong currency only one order is placed on the counterpart pair (for example, short `EURUSD`).
   - If USD is the weak currency the strategy buys the pair where the strong currency is the base (for example, long `EURUSD`).
   - When both currencies are non-USD the strategy sends two orders: long the top currency versus USD and short the down currency versus USD.
6. If `Close Opposite` is enabled and an opposite position is still open on a target pair, the strategy sends a closing market order before entering a fresh trade.

## Risk management
- The strategy does not attach explicit stop-loss or take-profit orders; risk control is handled by the `Close Opposite` flag together with manual portfolio management tools.
- Entry size is controlled by the `Volume` property. Configure it according to account size and desired exposure per leg.

## Differences vs the original MQL implementation
- The currency strength calculation uses StockSharp `SimpleMovingAverage` indicators on a single timeframe. Multi-timeframe coefficient stacking from the MQL indicator can be emulated by adjusting the `Fast MA` and `Slow MA` periods.
- Protective stops are not automatically trailed; instead, the strategy focuses on reproducing the entry/exit logic and leaves advanced risk control to StockSharp’s portfolio layer.
- Order routing uses the high-level `RegisterOrder` helper and StockSharp’s security references instead of MetaTrader trade objects.
