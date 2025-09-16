# BeerGod EMA Timing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the BeerGodEA MetaTrader expert advisor inside StockSharp. It trades mean-reversion setups on a single
symbol by monitoring a 60-period exponential moving average (EMA) and comparing the current price action with the previous bar.
Signals are evaluated only once per bar at a configurable minute offset after the candle opens, imitating the original EA that
waits a few minutes before acting.

When price temporarily breaks away from the EMA while the average is trending in the opposite direction, the strategy opens a
market position expecting the move to revert. Existing positions in the opposite direction are flipped immediately by adjusting
the order size so that shorts are covered before establishing a new long position (and vice versa).

## How It Works

1. Subscribe to time-frame candles (default 5 minutes) and build a 60-period EMA over the closing prices.
2. Track the current candle in real time. On the first tick of each new bar, store the previous EMA value and the prior bar close
   so the strategy can compare them later.
3. Once the configured number of minutes from the open elapses (default 3 minutes), evaluate the following conditions using the
   current price and EMA slope:
   - **Buy setup**: current price < current EMA, EMA is below its previous value (falling), and current price < previous bar close.
   - **Sell setup**: current price > current EMA, EMA is above its previous value (rising), and current price > previous bar close.
4. If a buy setup occurs while not already long, send a market buy order sized to close any open short and establish the desired
   long volume. The same logic applies symmetrically for sell setups.
5. After a trade is triggered, the signal for that candle is considered processed to prevent duplicate entries.

## Parameters

- **Volume** – order size in lots (default 1). The strategy automatically adds the absolute value of the current position when it
  needs to flip directions so that the new order closes the old exposure and opens the fresh trade in a single transaction.
- **EMA Length** – lookback period for the exponential moving average (default 60).
- **Trigger Minutes** – number of minutes after the bar opens when the entry conditions are checked (default 3). If the window is
  missed, the strategy waits for the next candle.
- **Candle Type** – candle data type used for calculations (default 5-minute time frame).

## Trading Notes

- The logic works on any symbol as long as candle data and level1 prices are available. Adjust the candle duration if the
  instrument trades on different sessions than the original MetaTrader setup.
- Only one position (long or short) is maintained at any moment. Flipping directions is done by sizing the new market order to
  cover the outstanding position and open the new trade in one step.
- No explicit stop-loss or take-profit levels are defined in the original EA. Risk management should be added externally if
  required.
- Start protection is enabled so that StockSharp automatically handles emergency position exits when manual intervention or
  connection issues occur.
