# MACD Signal ATR Strategy

## Overview
The **MACD Signal Strategy** ports the MetaTrader expert `MACD_signal.mq4` to StockSharp. The original robot measured the
MACD histogram against an ATR-based volatility band and opened a single market order whenever the histogram crossed that
band. This C# version recreates the same momentum breakout logic using StockSharp's high-level API, stores the previous
histogram and ATR readings explicitly, and documents every money-management rule with named parameters and English
comments in the source code.

Unlike the MetaTrader implementation that directly modified tickets, the StockSharp port works with net positions. It
therefore closes the current exposure before flipping direction and updates trailing stops internally instead of relying on
broker-side `OrderModify` calls.

## Trading logic
1. Subscribe to the configured candle series (`CandleType`) and process **only** finished candles to avoid partial-bar
   noise.
2. Feed a `MovingAverageConvergenceDivergenceSignal` indicator with the chosen fast, slow, and signal EMA lengths. The
   histogram value (`MACD - signal`) is stored every time a bar closes.
3. Compute the `AverageTrueRange` on the same candles. The value from the **previous** bar is multiplied by
   `ThresholdMultiplier` to recreate the `rr = ATR * LEVEL` threshold from MQL.
4. Detect a bullish breakout when the current histogram exceeds `+threshold` while the previous histogram was still below
   it. If the account is flat or short and long trading is allowed by `Direction`, send a market buy order sized by
   `TradeVolume`.
5. Detect a bearish breakout when the histogram crosses beneath `-threshold` after being above it on the prior candle. If
   the strategy is flat or long and short trading is enabled, issue a market sell order sized by `TradeVolume`.
6. Manage open positions every bar:
   - close longs as soon as the histogram turns negative; close shorts when it turns positive;
   - monitor the fixed take-profit distance (`TakeProfitPoints`) against candle highs or lows to emulate the original
     MetaTrader take-profit parameter;
   - update trailing stops once price moves more than `TrailingStopPoints` away from the entry and exit if the candle revisits
     the trailing level. The long stop trails the close as a proxy for the bid price, while the short stop trails the close as
     a proxy for the ask price.
7. The EA refuses to trade when `TakeProfitPoints` is below the historical 10-point minimum, matching the protective check
   present in the MQL code.

## Risk management
- **Single order at a time.** The strategy always net-flat before opening a new position, mirroring the original
  `OrdersTotal() < 1` requirement.
- **Fixed volume.** `TradeVolume` replaces the `Lots` input and is also copied to `Strategy.Volume` so manual UI actions use
  the same size.
- **Fixed take-profit.** `TakeProfitPoints` converts the MQL point distance to the instrument tick size using
  `Security.PriceStep`.
- **Indicator-based exit.** A histogram sign flip triggers an immediate market exit, guaranteeing the EA does not stay in
  the market when momentum reverses.
- **Trailing stop.** Once price moves in favour of the trade by more than the configured number of steps, the stop is pulled
  inside the profit zone and follows the close price while never moving backwards.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `10` | Order size (lots) used for every market entry and copied to `Strategy.Volume`. |
| `TakeProfitPoints` | `int` | `10` | Distance to the fixed take-profit target expressed in price steps. Values below 10
 disable trading. |
| `TrailingStopPoints` | `int` | `25` | Distance in price steps for the trailing stop. Set to `0` to disable trailing. |
| `FastPeriod` | `int` | `9` | Length of the fast EMA inside the MACD indicator. |
| `SlowPeriod` | `int` | `15` | Length of the slow EMA inside the MACD indicator. |
| `SignalPeriod` | `int` | `8` | Length of the EMA used to smooth the MACD signal line. |
| `ThresholdMultiplier` | `decimal` | `0.004` | Multiplier applied to the previous-bar ATR to build the breakout band. |
| `AtrPeriod` | `int` | `200` | Number of candles used to compute the ATR volatility filter. |
| `CandleType` | `DataType` | 30-minute timeframe | Primary timeframe processed by the strategy. |

## Differences from the original expert advisor
- MetaTrader exposes `AccountFreeMargin()` and refused to trade if the value was too small. StockSharp strategies do not
  have the same margin snapshot, so the port omits that check. Portfolio-level risk controls should be handled outside the
  strategy when required.
- The MQL version adjusted stop orders with `OrderModify`. StockSharp works with net positions, so the conversion manages
  exits internally by monitoring candle highs/lows and the trailing stop variables.
- MetaTrader counted "bars" manually and printed a warning when fewer than 100 candles were available. StockSharp relies on
  indicator readiness (`BindEx`) so the strategy stays idle automatically until MACD and ATR have enough data.
- The port stores the previous ATR and histogram values explicitly to reproduce the `Delta`/`Delta1` threshold comparison
  without violating StockSharp's rule against random indicator indexing.

## Usage tips
- Keep `Security.PriceStep`, `Security.MinVolume`, and `Security.VolumeStep` accurate so volume conversions and take-profit
  calculations remain aligned with the exchange.
- Increase `ThresholdMultiplier` or `AtrPeriod` when the strategy trades too frequently in choppy markets; decrease them to
  make the system more sensitive to volatility breakouts.
- Lower `TradeVolume` when running on leveraged or high-volatility instruments, because the original script assumed large
  lot sizes on Forex symbols.
- Combine the strategy with higher-timeframe filters through the built-in `Direction` property if you only want to allow
  longs or shorts during specific market regimes.
