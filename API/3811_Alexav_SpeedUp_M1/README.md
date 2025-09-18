# Alexav SpeedUp M1 Strategy

## Overview
The **Alexav SpeedUp M1 Strategy** is a direct port of the MetaTrader 4 expert advisor "Alexav_SpeedUp_M1". It evaluates the bodies of completed intraday candles and immediately reacts with market orders whenever the candle body exceeds a configurable threshold. After an entry the strategy emulates MetaTrader-style risk management by attaching stop-loss, take-profit and trailing stop orders to the open position.

The conversion relies on the StockSharp high level API. Candles are consumed through `SubscribeCandles`, price information for trailing is received from level1 data, and protective orders are placed with standard `BuyStop`, `SellStop`, `BuyLimit`, and `SellLimit` helpers. No manual indicator calculations are required.

## Signal generation
1. Each finished candle on the configured time frame is inspected.
2. When the candle closes above its open by more than **Body Threshold** the strategy opens (or reverses into) a long position at market.
3. When the candle closes below its open by more than the same threshold the strategy opens (or reverses into) a short position at market.
4. Existing exposure in the opposite direction is closed automatically by increasing the market order volume, faithfully reproducing the behaviour of the original expert advisor.

## Order management
* **Initial stop-loss**: As soon as the position volume increases, a protective stop order is registered at the entry price minus (for longs) or plus (for shorts) the configured number of points.
* **Take-profit**: A matching limit order is placed in the direction of the trade at the distance specified by **Take Profit (points)**.
* **Trailing stop**: Level1 bid/ask updates monitor the current profit. When the unrealised profit exceeds the trailing distance the protective stop is moved towards the price, maintaining the configured gap while never stepping backwards.
* All protective orders are cancelled whenever the position returns to flat.

The conversion keeps the logic intentionally simple: no additional filters, indicators, or risk controls are added beyond what was present in the MQL implementation.

## Parameters
| Name | Description |
| ---- | ----------- |
| **Lot Size** | Base trading volume (in lots) used for each market order. When reversing an existing position the required volume is added automatically. |
| **Take Profit (points)** | Distance from the entry price to the take-profit level measured in MetaTrader points (converted using the security price step). |
| **Initial Stop (points)** | Distance from the entry price to the initial protective stop expressed in points. |
| **Trailing Stop (points)** | Trailing distance maintained after the price moves in favour of the position. A value of zero disables the trailing logic. |
| **Body Threshold** | Minimum absolute difference between candle close and open required to trigger a new trade. |
| **Candle Type** | Candle series (time frame) used for signal evaluation. The default matches the original one-minute chart. |

## Usage notes
* Ensure that the security provides a valid `PriceStep`. When unavailable the strategy falls back to interpreting point distances as raw price offsets.
* The trailing stop logic requires level1 data (best bid/ask). When only candle data is available the trailing functionality remains dormant.
* The strategy is designed for intraday operation and mirrors the one-trade-per-candle behaviour enforced by the original MQL expert via its internal counters.
