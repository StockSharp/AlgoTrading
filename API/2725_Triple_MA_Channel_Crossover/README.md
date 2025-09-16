# Triple MA Channel Crossover Strategy

## Overview
The **Triple MA Channel Crossover Strategy** trades directional breakouts when a fast moving average moves through both a middle
and a slow moving average. A Donchian-style price channel is used to manage exits and to provide optional automatic stop-loss and
take-profit levels. The conversion is based on the original MetaTrader "3MACross EA" and keeps its configurable moving average
structure, risk controls and trailing logic.

The strategy scales in up to a configurable number of positions, supports manual pip-based risk targets, and can follow the
channel for adaptive exits. When enabled, the break-even trigger pushes the stop loss to the entry price plus a safety buffer.

## Trading Logic
- **Entry conditions**
  - *Long:* the fast moving average crosses above both the middle and slow averages. If `Trade On Close` is enabled the cross must
    occur on a fully closed candle; otherwise the long signal is allowed while the fast average stays above both slower averages.
  - *Short:* the fast moving average crosses below the middle and slow averages with the same confirmation logic.
  - Existing positions on the opposite side are closed and reversed immediately. Scaling into the same direction is allowed until
    `Max Positions` is reached.
- **Exit conditions**
  - Price reaching the configured take-profit or channel-based target.
  - Price touching the dynamic stop level (manual distance, trailing stop, break-even movement or channel-based stop).
  - Optional trailing stop adjusts after the price moves in favor by at least the trailing step distance.

## Risk Management
- Stops and targets can be defined manually in pips or derived from the price channel when `Auto SL/TP` is enabled.
- Trailing stop and break-even logic mirror the original expert advisor. The stop moves only in the favorable direction and is
  never relaxed.
- The Donchian channel provides natural support/resistance bounds that can be used for automatic stop-loss and take-profit
  placement.
- `Max Positions` limits the number of scaling steps, preventing uncontrolled pyramiding.

## Key Parameters
| Parameter | Description |
|-----------|-------------|
| `Volume` | Order size for each scaling step. |
| `Stop Loss (pips)` | Fixed distance for the protective stop. Set to `0` to disable. |
| `Take Profit (pips)` | Fixed distance for the profit target. Set to `0` to disable. |
| `Trailing Stop (pips)` | Distance used by the trailing stop. `0` disables trailing. |
| `Trailing Step (pips)` | Minimum advance required before updating the trailing stop. |
| `Break Even (pips)` | Profit required before locking in a break-even stop. |
| `Auto SL/TP` | Use the Donchian channel instead of fixed distances for stop-loss and take-profit placement. |
| `Trade On Close` | Require crossovers to be confirmed on a closed candle. If disabled, alignment of averages is checked each bar. |
| `Max Positions` | Maximum number of scaling steps per direction. |
| `Fast/Middle/Slow MA Period` | Length of the moving averages. |
| `Fast/Middle/Slow MA Shift` | Optional shift (in bars) applied to each moving average. |
| `Fast/Middle/Slow MA Type` | Moving average calculation mode (Simple, Exponential, Smoothed, Weighted). |
| `Channel Period` | Lookback for the Donchian channel high/low. |
| `Candle Type` | Timeframe of the candles processed by the strategy. |

## Implementation Notes
- Pip distances are converted using `Security.PriceStep`. For instruments without a valid tick size the strategy falls back to a
  distance of `1` price unit per pip.
- Automatic channel management keeps stop-loss and take-profit levels only moving closer to the current price; they are never
  widened.
- Break-even activation reuses the trailing step as an additional buffer, matching the original EA behaviour.
- The strategy is designed for use with StockSharp high-level APIs and handles chart rendering (MAs and Donchian channel) for
  visual analysis.
- Ensure historical data depth is sufficient for the slow moving average and channel period so that crossover signals are valid.

## Usage
1. Attach the strategy to a security and set the desired candle timeframe.
2. Configure moving average periods/methods to match the original EA or your adaptation.
3. Choose between manual pip-based risk settings or enable automatic channel exits.
4. Start the strategy; it will subscribe to the configured candles, calculate indicators and trade when the crossover conditions
   are met.
5. Monitor the trailing stop and break-even adjustments through the logs and chart overlays.

> **Disclaimer:** Automated trading involves significant risk. Test the strategy thoroughly with historical data and in a
> simulation environment before deploying to live markets.
