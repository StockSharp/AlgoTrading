# Stochastic Accelerator Strategy

## Overview
The Stochastic Accelerator strategy is a conversion of the MetaTrader 5 expert *#2 stoch mt5*. The original robot evaluates three
stochastic oscillators together with Bill Williams' Accelerator Oscillator and the Awesome Oscillator. A long position is opened
only when all stochastic filters agree on bullish momentum and the Accelerator Oscillator crosses above a sensitivity threshold.
Short positions use the symmetrical rules. Once a trade is running, the Awesome Oscillator monitors momentum reversals to close
the exposure. The StockSharp port reproduces these mechanics while relying on the high-level candle subscription API and
indicator bindings.

The strategy keeps the money-management profile from the EA. Entries are sized with a fixed lot amount, while stop-loss and
take-profit distances are expressed in MetaTrader pips. The StockSharp implementation uses `StartProtection` so the configured
risk limits are attached automatically to every new position. Price steps are converted to MetaTrader pip units to maintain the
same protective distances across brokers.

## Trading logic
1. Subscribe to the primary candle series defined by `CandleType` and process only finished candles, mirroring the original EA.
2. Feed three `StochasticOscillator` instances:
   - The **signal stochastic** checks whether %K is above or below %D.
   - The **entry stochastic** validates that bullish signals stay above `EntryLevel` (or below `100 - EntryLevel` for shorts).
   - The **filter stochastic** ensures that bullish setups remain under `FilterLevel` (or above `100 - FilterLevel` for shorts).
3. Track the Accelerator Oscillator and require that it crosses above `AcceleratorLevel` to confirm long entries. Shorts demand a
   cross below `-AcceleratorLevel`.
4. Close any open position when the Awesome Oscillator crosses back through the `AwesomeLevel` band in the opposite direction.
5. After flattening, open a new position if exactly one side satisfies all entry filters. The volume is adjusted to the security's
   lot step so the request remains valid for real brokers.
6. Apply stop-loss and take-profit distances using `StartProtection`, keeping the same pip-based risk controls as the MetaTrader
   expert.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 4-hour time frame | Primary candles processed by the strategy. |
| `TradeVolume` | `decimal` | `0.01` | Volume used for new entries (lots). |
| `StopLossPips` | `decimal` | `40` | Stop-loss distance in MetaTrader pips. |
| `TakeProfitPips` | `decimal` | `70` | Take-profit distance in MetaTrader pips. |
| `SignalKPeriod` | `int` | `40` | %K period of the confirmation stochastic. |
| `SignalDPeriod` | `int` | `10` | %D smoothing of the confirmation stochastic. |
| `SignalSlowing` | `int` | `10` | Additional smoothing for the confirmation stochastic. |
| `EntryKPeriod` | `int` | `40` | %K period of the entry stochastic. |
| `EntryDPeriod` | `int` | `10` | %D smoothing of the entry stochastic. |
| `EntrySlowing` | `int` | `10` | Additional smoothing for the entry stochastic. |
| `EntryLevel` | `decimal` | `20` | Lower threshold that confirms bullish momentum (shorts use `100 - EntryLevel`). |
| `FilterKPeriod` | `int` | `40` | %K period of the filter stochastic. |
| `FilterDPeriod` | `int` | `10` | %D smoothing of the filter stochastic. |
| `FilterSlowing` | `int` | `10` | Additional smoothing for the filter stochastic. |
| `FilterLevel` | `decimal` | `75` | Upper threshold limiting bullish setups (shorts use `100 - FilterLevel`). |
| `AcceleratorLevel` | `decimal` | `0.0002` | Minimum Accelerator Oscillator amplitude required for entries. |
| `AwesomeLevel` | `decimal` | `0.0013` | Awesome Oscillator band that triggers trade exits. |

## Differences from the original MetaTrader expert
- The StockSharp port uses candle subscriptions with indicator bindings instead of repeated `CopyBuffer` calls.
- Order management is performed in net position mode. When the EA would reverse immediately, the conversion first closes the
  current exposure and then issues a new market order on the opposite side.
- Stop-loss and take-profit distances are attached via `StartProtection`, using pip-size calculations derived from the
  instrument's price step. This avoids manual ticket modifications while keeping the distances identical to MetaTrader points.
- Volume requests are normalized to the security's `VolumeStep`, `MinVolume`, and `MaxVolume` so the code is ready for live
  trading environments.

## Usage tips
- Adjust `TradeVolume` to match the instrument's minimum lot step before running the strategy.
- Fine-tune the stochastic levels (`EntryLevel` and `FilterLevel`) together with the oscillator thresholds to adapt the filter
  strictness to your market.
- Enable chart drawing when available to visualize the three stochastic oscillators, the Accelerator Oscillator, the Awesome
  Oscillator, and executed trades.
- Because the logic waits for finished candles, signals appear at the close of each bar; use a backtester with the same timeframe
  for consistent results.

## Indicators
- Three `StochasticOscillator` instances with independent smoothing and threshold settings.
- `AcceleratorOscillator` for entry confirmation.
- `AwesomeOscillator` for exit timing.
