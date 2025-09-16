# Order Manager

This strategy monitors open portfolio positions and automatically closes them when unrealized profit or loss reach specified fractions of the account balance.

## Parameters
- **Manage All Securities** – if true, apply rules to every position in the portfolio. Otherwise only the strategy security is managed.
- **Use Stop Loss** – enable exit when loss exceeds the allowed fraction.
- **Stop Loss %** – loss percentage of the portfolio value that triggers closing the position.
- **Use Take Profit** – enable exit when profit reaches the target fraction.
- **Take Profit %** – profit percentage of the portfolio value required to close the position.

## Logic
1. Subscribes to trade ticks to obtain current prices.
2. For each monitored position, calculates unrealized profit in money terms.
3. Converts this profit to a fraction of the current portfolio value.
4. If loss is greater than the stop level or profit reaches the take level, the position is closed with a market order.

The strategy does not open new positions and acts purely as a portfolio risk manager.
