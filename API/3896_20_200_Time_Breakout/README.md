# Twenty200 Time Breakout

This strategy is a StockSharp port of the MetaTrader expert advisor **20/200 expert v4.2 (AntS)**. It waits for a specific hour of the trading day and then compares two historical hourly open prices (6 and 2 bars back in the default configuration). If the distant open is higher than the nearer open by more than `Short Delta` pips the strategy sells, while the reverse gap that exceeds `Long Delta` pips opens a long position.

## Trading logic

- The strategy subscribes to hourly candles (configurable through `Candle Type`).
- Only one trade per day is allowed. Orders are placed when a candle with hour equal to `Trade Hour` becomes active.
- Signals use the open price `LookbackFar` and `LookbackNear` bars back from the current candle.
  - **Short setup:** `Open[t1] - Open[t2] > Short Delta × pip`.
  - **Long setup:** `Open[t2] - Open[t1] > Long Delta × pip`.
- A market order is sent with the calculated volume. Stop-loss and take-profit distances are taken from the MetaTrader version and expressed in pips, automatically converted to prices via `Security.PriceStep`.
- Only one position can exist at a time. Daily trading resumes on the next calendar day.

## Position management

- Stop-loss and take-profit are evaluated on every candle update using candle high/low extremes.
- `Max Open Hours` forces a market exit when the position lifetime exceeds the configured number of hours (504 hours by default). Set the parameter to zero to disable the safety timer.

## Money management

- `Fixed Volume` defines the fallback contract size used when `Use Auto Lot` is disabled or the balance information is unavailable.
- When `Use Auto Lot` is enabled the lot size follows the enormous step table from the expert advisor. In StockSharp the table is approximated by `volume = round(balance × Auto Lot Factor, 2)` with the default factor `0.000038`, reproducing the MT4 values within one pip of volume across the documented range (300 USD to 270,000 USD+).
- If the current portfolio value drops below the last recorded balance the next trade is multiplied by `Big Lot Multiplier`, mimicking the "Big Lot" recovery trade in the original code.
- Volumes are aligned to `Security.VolumeStep` and clamped between `MinVolume`/`MaxVolume` when available.

## Differences vs. the MetaTrader EA

- The MT4 script stored more than a thousand manual threshold rows. The StockSharp version uses a linear coefficient (`Auto Lot Factor`) that fits the same staircase. Adjust the factor if you need an exact replica for a different broker.
- Stop-loss/take-profit orders are simulated through market exits on candle extremes. This keeps behaviour consistent across backtests and live trading without depending on exchange-side stop order support.
- Global variables (`globalBalans`, `globalPosic`) are replaced with in-memory state. No file system or terminal state is required.

## Parameters

| Parameter | Description |
|-----------|-------------|
| Long/Short Take Profit | Distance in pips for profit targets. |
| Long/Short Stop Loss | Distance in pips for stop losses. |
| Trade Hour | Hour of the session (0–23) when signals may trigger. |
| Far/Near Lookback | How many bars back to inspect for the two open prices. |
| Long/Short Delta | Required pip gap to open a position. |
| Max Open Hours | Maximum position lifetime in hours (0 disables the guard). |
| Fixed Volume | Baseline contract volume when auto sizing is disabled. |
| Use Auto Lot | Enable lot sizing from account value. |
| Auto Lot Factor | Multiplier applied to portfolio value to emulate the MT4 step table. |
| Big Lot Multiplier | Volume multiplier applied after an equity drop. |
| Candle Type | Time frame used for the signal candles. |
