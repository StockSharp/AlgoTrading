# Exp Blau CSI Strategy

This strategy is a C# conversion of the MetaTrader 5 expert adviser `Exp_BlauCSI`. It trades on the Blau Candle Stochastic Index (CSI) calculated on a selected candle series. The strategy can react either to zero-line breakdowns or to direction changes in the indicator and supports configurable stop-loss and take-profit levels measured in price steps.

## Trading logic

The Blau CSI compares a momentum component with the high-low range of recent candles. Both parts are smoothed three times using a selected moving average type.

* **Breakdown mode** – opens a long position when the indicator crosses below zero and closes any shorts while the previous value was positive. Opens a short position on a cross above zero and closes any longs while the previous value was negative.
* **Twist mode** – opens a long position when the indicator turns upward (value rises compared to the previous bar after declining). Opens a short position when the indicator turns downward. The previous bar direction is always used to close existing positions for the opposite side.

Signals are evaluated on a configurable historical bar (`Signal Bar`) to ensure confirmation on fully closed candles.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Entry Mode` | Selects `Breakdown` or `Twist` logic. |
| `Smoothing Method` | Moving average type used inside the Blau CSI (Simple, Exponential, Smoothed, LinearWeighted, or Jurik). |
| `Momentum Length` | Number of bars used to compute the momentum and range components. |
| `First/Second/Third Smoothing` | Depth of the three smoothing stages applied to momentum and range. |
| `Smoothing Phase` | Phase parameter for Jurik smoothing (ignored by other methods). |
| `Momentum Price` / `Reference Price` | Applied price constants for the leading and lagging momentum values (close, open, high, low, median, typical, weighted, simple, quarter, trend-following, or Demark). |
| `Signal Bar` | Offset (in bars) used when evaluating the Blau CSI buffer. Default `1` means the previous closed bar. |
| `Stop Loss (pts)` | Stop-loss distance in price steps (`0` disables). |
| `Take Profit (pts)` | Take-profit distance in price steps (`0` disables). |
| `Allow Long/Short Entries` | Enable or disable opening positions for each direction. |
| `Allow Long/Short Exits` | Enable or disable exit signals for existing positions. |
| `Candle Type` | Data type for the subscription (defaults to 4-hour time frame). |
| `Start Date` / `End Date` | Date filters for trading activity. |
| `Order Volume` | Market order volume. |

## Risk management

When a new position is opened the strategy calculates stop-loss and take-profit levels using the instrument `PriceStep`. If the instrument does not provide a step, stops are disabled automatically. Trailing is not performed; each position keeps the initial protective levels until it is closed by a signal or by reaching a target.

## Usage notes

1. Attach the strategy to a security that provides candle data for the selected `Candle Type`.
2. Choose the indicator mode and smoothing parameters according to your trading plan.
3. Ensure the instrument has a valid `PriceStep` when using stop-loss or take-profit distances.
4. Optionally restrict trading to a time range using `Start Date` and `End Date`.

## Differences compared to the original MT5 version

* The implementation uses StockSharp indicators and C# strategy APIs instead of MetaTrader trading functions.
* Lot size management is simplified: order volume is taken directly from the `Order Volume` parameter.
* Only the smoothing methods provided by StockSharp are supported (Simple, Exponential, Smoothed, LinearWeighted, Jurik). Unsupported MT5 modes fall back to Exponential smoothing.
* Trade direction toggles and stop management remain compatible with the original behavior.

The strategy is ready for backtesting within StockSharp Designer, Shell, Runner, or any custom StockSharp host application.