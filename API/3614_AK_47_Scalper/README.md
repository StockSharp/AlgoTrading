# AK-47 Scalper Strategy

This strategy is a conversion of the MetaTrader 5 expert advisor **"AK-47 Scalper EA" (build 44883)**. It recreates the original behaviour inside the StockSharp high-level strategy framework.

The algorithm keeps a single *sell stop* order active during the allowed trading hours. Once the order is triggered the strategy immediately attaches protective stop-loss and take-profit orders. Both the pending order price and the protective stop are tightened dynamically as the market moves.

## Core Logic

1. Calculate the pip size from the instrument tick size (5-digit symbols use 0.1 pip steps just like in MetaTrader).
2. Determine the trading window. When the time filter is enabled, entries are allowed only between the configured start and end times (inclusive of the start, exclusive of the end). Overnight sessions are supported by wrapping around midnight.
3. Make sure the current spread in points does not exceed the configured limit before placing new orders.
4. Size the position:
   - Either use the fixed lot (`Base Lot` parameter), or
   - Convert the configured `Risk Percent` of the portfolio value into lots (mimicking the MT5 formula) and align it with the exchange volume constraints.
5. Place a sell stop order `SL/2` pips below the bid. The protective stop is planned `SL/2` pips above the ask and the take profit sits `TP` pips below the entry.
6. While the order is pending the strategy continually re-registers it to keep the SL/2 pip gap to the bid and updates the planned protective prices.
7. After execution:
   - Register a buy-stop stop-loss order and a buy-limit take-profit order using the planned prices.
   - On every candle close the strategy trails the stop by keeping it exactly `SL` pips above the current bid (never loosening it).
   - The take-profit price stays fixed once set.
8. If the position is flat all protective orders are cancelled and a new cycle can start.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Use Risk Percent** | Switch between fixed lots and equity-based sizing. |
| **Risk Percent** | Percentage applied to the portfolio value when calculating the trade volume. |
| **Base Lot** | Fixed lot size and rounding step for the position sizing. |
| **Stop Loss (pips)** | Distance between the entry price and the protective stop. The pending order offset uses half of this distance. |
| **Take Profit (pips)** | Profit target distance. Set to zero to disable the target. |
| **Max Spread (points)** | Maximum allowed spread (in MetaTrader points) to enter the market. |
| **Use Time Filter** | Enable or disable the trading window restriction. |
| **Start Hour / Minute** | Beginning of the trading window. |
| **End Hour / Minute** | End of the trading window. |
| **Candle Type** | Candle subscription used for timing and price updates. |

## Notes

- The strategy uses only short entries just like the original EA.
- Trailing is performed on candle close to stay in sync with the StockSharp high-level API.
- Protective orders are replaced via `ReRegisterOrder` calls, so the exchange or simulator must support order replacement.
- The original graphical comments from MetaTrader are not reproduced because StockSharp strategies rely on logging instead of terminal comments.
