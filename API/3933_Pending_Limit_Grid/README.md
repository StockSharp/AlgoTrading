# Pending Limit Grid Strategy (MQL/8147 Conversion)

## Overview

The **Pending Limit Grid Strategy** reproduces the behaviour of the MetaTrader expert
stored in `MQL/8147`. The strategy builds a symmetric grid of pending limit orders
around the current bid/ask prices. It keeps the grid active while floating profit
remains within a configured profit target and drawdown threshold. When one of the
thresholds is breached, all orders are cancelled, open positions are flattened, and
the grid is rebuilt using the new account equity as baseline.

## Trading Logic

1. Subscribe to level one data to track the best bid and ask prices.
2. Capture the account equity the first time live data is received and store it as
   the session baseline.
3. Place `LevelsPerSide` sell limits above the market and the same number of buy
   limits below the market. The distance between grid levels is controlled by
   `GridStepPoints` converted to the instrument price step.
4. Hold the pending orders without reissuing new ones when they are filled. The
   grid is recreated only after a full reset.
5. Continuously monitor floating PnL:
   - If profit reaches `ProfitTargetCurrency`, close all exposure and reset.
   - If drawdown exceeds `MaxDrawdownCurrency`, flatten the book and reset.
6. After every reset the baseline equity is captured again and the grid is rebuilt
   using the most recent bid/ask snapshot.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `ProfitTargetCurrency` | Net profit (in account currency) that triggers a full reset of the grid. |
| `MaxDrawdownCurrency` | Maximum tolerated floating loss before all exposure is closed. |
| `GridStepPoints` | Distance between consecutive grid levels expressed in broker points. |
| `LevelsPerSide` | Number of pending orders created above and below the market. |
| `OrderVolume` | Volume assigned to each pending limit order. |

## Risk Management

The strategy does not attach per-order stops or targets. Instead it supervises the
aggregated profit and loss. The `RequestFlatten` helper cancels pending orders and
uses market orders (via `ClosePosition`) to remove any open exposure. After the
flattening completes, the grid state and baseline equity are reset before placing
new orders.

## Notes

- Prices are normalised through `Security.ShrinkPrice` to respect the exchange
  price step.
- The MetaTrader "Point" value is emulated by analysing the instrument `PriceStep`
  to match four- and five-digit quotes.
- The strategy avoids re-sending grid orders once they are placed, mimicking the
  original expert that relied on flag variables to keep every level unique until
a manual or automatic reset occurs.
