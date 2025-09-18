# RSI Trader Strategy

## Summary
This strategy reproduces the "RSI trader" MetaTrader Expert Advisor. It aligns two trend filters – price moving averages and smoothed RSI averages – to enter in the direction of the dominant trend and exits when the filters diverge (sideways regime). The StockSharp port works on any instrument with candle data support and defaults to hourly candles as in the original description.

## How it works
1. Calculate RSI with the period specified by **RSI Period** (default 14).
2. Smooth the RSI stream with two simple moving averages: a short one (**Short RSI MA**) and a long one (**Long RSI MA**).
3. Smooth closing prices with two moving averages: a short simple MA (**Short Price MA**) and a long linear-weighted MA (**Long Price MA**).
4. Generate signals on finished candles only:
   - **Long** – both short averages (price and RSI) are above their long counterparts.
   - **Short** – both short averages are below their long counterparts.
   - **Sideways** – the averages disagree (one indicates uptrend and the other downtrend). When this occurs any open position is closed.
5. Orders are issued with `BuyMarket` / `SellMarket`. Opposite positions are flattened before entering a new direction.

## Parameters
| Name | Description | Default | Optimizable |
| --- | --- | --- | --- |
| `RSI Period` | RSI calculation length. | 14 | Yes (7…28, step 1) |
| `Short Price MA` | Length of the short simple moving average of price. | 9 | Yes (5…20, step 1) |
| `Long Price MA` | Length of the long linear-weighted moving average of price. | 45 | Yes (30…90, step 5) |
| `Short RSI MA` | Length of the short smoothing average applied to RSI. | 9 | Yes (5…20, step 1) |
| `Long RSI MA` | Length of the long smoothing average applied to RSI. | 45 | Yes (30…90, step 5) |
| `Candle Type` | Data type used for candles. Defaults to 1-hour timeframe. | H1 | No |

## Notes
- Trading is only performed when all indicators are formed.
- The original EA used lots and slippage settings. StockSharp uses the strategy `Volume` property for order size and leaves execution slippage management to the trading adapter.
- No built-in stop-loss or take-profit is defined; exits depend on sideways detection. Additional risk management can be added externally.
- Charts draw both price and RSI moving averages when the charting service is available.
