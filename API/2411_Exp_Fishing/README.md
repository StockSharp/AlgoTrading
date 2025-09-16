# Exp Fishing Strategy

This strategy enters a position when the completed candle's close differs from its open by at least **Price Step**. If the difference is positive, it buys; if negative, it sells.

After opening a position, every additional move of **Price Step** in the trade's favor triggers an additional market order in the same direction up to **Max Orders**. Protective stop-loss and take-profit are applied for every position using absolute price distances.

## Parameters

- **Price Step** – minimum price move (in absolute units) required to open or add to a position.  
- **Max Orders** – maximum number of market orders allowed in one direction.  
- **Stop Loss** – distance from entry price where a protective stop is placed.  
- **Take Profit** – distance from entry price where a profit target is placed.  
- **Candle Type** – candle timeframe used for calculations (default 1 minute).

## Trading Logic

1. Wait for a finished candle.
2. If no position is open:
   - Buy if `Close - Open >= Price Step`.
   - Sell if `Open - Close >= Price Step`.
3. When a position exists:
   - If price advances by `Price Step` from last entry, add another order in the same direction.
   - Stop adding orders once the number reaches **Max Orders**.
4. Stop-loss and take-profit are managed automatically for each order.

The strategy is adapted from the MQL5 expert "Exp Fishing" and demonstrates a simple grid-style trend-following approach.
