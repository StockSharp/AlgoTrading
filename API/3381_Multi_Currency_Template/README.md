# Multi Currency Template Strategy

## Overview
The **Multi Currency Template Strategy** is a conversion of the MetaTrader 4 expert advisor *Multi Currency Template v4*. It reproduces the original EMA crossover entry logic together with martingale style averaging, pip based protective levels and trailing management using the StockSharp high-level API. The default time frame is five-minute candles, but it can be changed through a parameter.

## Trade Logic
- Two exponential moving averages (EMA 20 and EMA 50) are calculated on every finished candle of the selected time frame.
- A long signal appears when the fast EMA (20) closes above the slow EMA (50). A short signal appears when the fast EMA closes below the slow EMA.
- The `Order Method` parameter decides whether the strategy acts on both signals or restricts trading to long-only or short-only operation.
- Only one net position per direction is maintained. When a new signal arrives, the strategy closes any opposite position before opening the requested side.

## Position Management
- **Stop Loss / Take Profit** – distances are entered in MetaTrader pips. They are converted to price units using the security price step, reproducing the original handling of 4- and 5-digit Forex symbols.
- **Trailing Stop** – activates once price moves in favor of the position by `Trailing Stop (pts)` and is tightened after every additional improvement of `Trailing Step (pts)`.
- **Martingale Averaging** – when enabled, additional market orders are sent every `Step (pts)` against the current position. Each new order volume is scaled by `Lot Multiplier` and the process repeats until the position is closed.
- **Average Take Profit** – when two or more averaging orders are open, the take profit target can optionally use the weighted position price plus `Average TP Offset (pts)` to emulate the MetaTrader “TP average” behaviour.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| Order Method | Trade direction (Buy & Sell, Buy only, Sell only). | Buy & Sell |
| Volume (lots) | Base market order size. | 0.01 |
| Stop Loss (pips) | Protective stop distance in MetaTrader pips. | 50 |
| Take Profit (pips) | Profit target distance in MetaTrader pips. | 100 |
| Trailing Stop (pts) | Activation threshold for the trailing stop in MetaTrader points. | 15 |
| Trailing Step (pts) | Minimal improvement needed before the trailing stop is moved. | 5 |
| Enable Martingale | Enables averaging down/up with increasing volume. | true |
| Lot Multiplier | Volume multiplier applied to every new averaging order. | 1.2 |
| Step (pts) | MetaTrader point distance before placing the next averaging order. | 150 |
| Average Take Profit | Switch between fixed or averaged take profit when multiple orders exist. | true |
| Average TP Offset (pts) | MetaTrader point offset applied to the averaged take profit. | 20 |
| Candle Type | Candle type (time frame) used for indicator calculations. | 5-minute candles |

## Differences from the Original Expert Advisor
- StockSharp executes net positions instead of managing individual MetaTrader tickets. The martingale module increases the net position size rather than attaching separate ticket-specific targets.
- Multi-symbol trading has to be achieved by launching several strategy instances, one per security. The original expert advisor supported a built-in multi-currency list inside one EA instance.
- Money management checks (`CheckMoneyForTrade`, `CheckVolumeValue`) and broker specific restrictions are replaced by StockSharp order validation.

## Usage Notes
1. Ensure that the security metadata (price step and decimals) match the instrument so pip conversion remains accurate.
2. Trailing stop and martingale logic act on candle close prices by default. For more reactive behaviour hook additional data sources (quotes or trades) and call the management helpers from there.
3. Because market orders are used, slippage control is delegated to the connected broker or simulator.
