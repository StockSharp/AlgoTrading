# Doji Trader Strategy

This strategy converts the MQL4 "DojiTrader" expert advisor into a StockSharp C# sample. It searches for recent doji candles and trades a breakout of the doji range during the main European and U.S. sessions.

## Trading logic
- The strategy processes only finished candles from the selected timeframe (30-minute candles by default).
- Trading is allowed only between 08:00 and 17:00 platform time.
- While flat, it looks back up to three completed candles and remembers the most recent doji (open price equals close price).
- When the candle immediately following the doji closes above the doji high, a long breakout is armed. If it closes below the doji low, a short breakout is armed.
- As soon as a subsequent candle closes beyond the arming price, the strategy sends a market order in the breakout direction.
- After entry the doji range is kept for exit control. The position is closed when:
  - The previous candle closes back inside the range (long: close below the doji low, short: close above the doji high).
  - The candle extremes reach the synthetic stop loss or take profit levels that mimic the original MQL4 fixed-point exits.

## Parameters
- **Order volume** – volume used for market orders.
- **Take profit (steps)** – distance to the profit target measured in price steps.
- **Stop loss (steps)** – distance to the protective stop in price steps.
- **Candle type** – timeframe of candles used for signal detection.

The stop-loss and take-profit calculations rely on the security price step, emulating the original EA that used fixed pip distances. When no valid doji is present within the last three candles, the breakout state is cleared and the search restarts.
