# Master Mind Triple WPR Strategy

## Overview
- Port of the MetaTrader 4 expert advisor `MasterMind3CE` (folder `MQL/8458`).
- Uses four Williams %R indicators with periods 26, 27, 29 and 30 to detect extreme overbought / oversold conditions.
- Designed for mean-reversion entries: buy after a deep sell-off, sell after an overextended rally.
- Includes configurable stop-loss, take-profit and optional trailing-stop logic expressed in instrument price steps.
- Works on any timeframe supported by the connected StockSharp terminal; default is 15-minute candles.

## Trading Logic
### Indicators
- `WilliamsR(26)` — extremely fast oscillator.
- `WilliamsR(27)` — fast oscillator for confirmation.
- `WilliamsR(29)` — medium oscillator that smoothes the signal.
- `WilliamsR(30)` — slow oscillator that requires extreme values across multiple lookbacks.

All four oscillators must be formed. The subscription processes only finished candles to match the original expert's `TradeAtCloseBar = true` behaviour.

### Entry Conditions
- **Long entry**: All four Williams %R values are below or equal to `OversoldLevel` (default `-99.99`). The strategy targets a long position of `TradeVolume`. If a short is open it is closed and flipped to long in a single market order sized to reach the target exposure.
- **Short entry**: All four Williams %R values are above or equal to `OverboughtLevel` (default `-0.01`). The strategy targets a short position of `TradeVolume`, closing any existing long exposure first.

### Exit Conditions
- **Signal-based exit**: When a long is open and a short entry condition appears the strategy closes/flip the position (and vice versa).
- **Protective stop-loss**: Optional price step distance applied from the average entry price. A hit on the candle's high/low triggers a market exit.
- **Take-profit**: Optional price step target from the average entry price. Once reached on the candle the position is closed.
- **Trailing-stop**: Optional trailing logic which starts once price moves by `TrailingStopSteps + TrailingStepSteps` in favour. The stop is then maintained `TrailingStopSteps` away from the latest close and only advances when improved by at least `TrailingStepSteps`.

## Risk Management
Price distances are specified in instrument *price steps*. For example, with `PriceStep = 0.0001` and `StopLossSteps = 2000`, the stop is placed 0.2000 away from the entry. The strategy recalculates average entry price when scaling into the same direction to keep risk levels consistent. Trailing stops are disabled unless both `TrailingStopSteps` and `TrailingStepSteps` are positive.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Target net position size (lots/contracts). | `1` |
| `OversoldLevel` | Williams %R threshold that confirms oversold conditions. | `-99.99` |
| `OverboughtLevel` | Williams %R threshold that confirms overbought conditions. | `-0.01` |
| `StopLossSteps` | Stop-loss distance in `PriceStep` units. Set `0` to disable. | `2000` |
| `TakeProfitSteps` | Take-profit distance in `PriceStep` units. Set `0` to disable. | `0` |
| `TrailingStopSteps` | Trailing-stop distance in `PriceStep` units. Requires `TrailingStepSteps > 0`. | `0` |
| `TrailingStepSteps` | Minimum improvement before the trailing stop is moved (in `PriceStep` units). | `1` |
| `CandleType` | Candle data type / timeframe processed by the strategy. | `TimeFrame(15m)` |

## Conversion Notes
- Alerts, sound notifications, logging to files and email features from the MQL expert are intentionally omitted; StockSharp logs can be used instead.
- The original advisor allowed trading before the bar close. The port keeps the default "trade on close" logic by processing only finished candles.
- Magic numbers, repeated order retries, and manual object drawing were specific to MetaTrader and have no direct StockSharp equivalents, so they are removed.
- Risk management is consolidated inside the strategy rather than using external order modification loops; stop/take checks are evaluated on each candle.

## Usage
1. Configure the desired instrument and timeframe, matching the chart the expert was originally attached to.
2. Adjust thresholds or risk parameters if the instrument has a different volatility profile.
3. Launch the strategy; it will subscribe to the specified candle series, monitor Williams %R extremes and manage positions accordingly.
