# New Martin Strategy

## Overview
New Martin Strategy replicates the original MetaTrader "New Martin" expert advisor by running a symmetric martingale hedge on both sides of the market. The strategy keeps an initial long and short position open at all times and rebalances the hedge when the fast and slow smoothed moving averages (SMMA) cross. When one side of the hedge is losing, the algorithm multiplies the exposure on that side and simultaneously realizes gains on the most profitable leg. Take-profit exits recycle the hedge by reopening the missing side and optionally purging both the best and worst performers to keep the grid compact.

The implementation targets StockSharp's high-level API and expects a portfolio with hedging support so that long and short legs can coexist. Orders are sent as market orders for simplicity, mirroring the original MQL logic where fills are assumed immediate.

## Indicators and Signals
- **Fast SMMA (default length 5):** tracks the short-term price direction.
- **Slow SMMA (default length 20):** represents the dominant trend.
- **Cross detection:** a crossover of the previous two completed bars triggers the martingale addition on the worst performing leg. The signal is throttled to once per candle by storing the candle open time of the last crossover.

## Position Management
- **Initial hedge:** as soon as indicators are formed the strategy opens one long and one short position with the configured initial volume. Both trades use a symmetric take-profit distance in pips.
- **Take-profit recycling:** when price touches the take-profit level of any leg, the strategy closes that position, records the event, and optionally closes both the most profitable and the most losing remaining positions to realize profits and losses in pairs. Missing sides are immediately reopened with the base volume so the hedge remains balanced.
- **Martingale averaging:** on every SMMA crossover the algorithm identifies the position with the lowest unrealized profit. It increases exposure on that side by multiplying the trade volume with the martingale multiplier (default 1.6) after adjusting to the security volume step. The most profitable open position is closed right after the averaging trade to release locked profit.

## Risk Management
- **Equity drawdown guard:** the highest observed portfolio equity is tracked. If the drawdown from that peak exceeds the configured percentage, all open positions are liquidated and the hedge initialization is postponed until the next candle.
- **Dynamic base volume:** whenever equity grows by at least the martingale multiplier relative to the previously recorded balance, the base hedge volume is increased by the same multiplier (again respecting exchange volume limits). This mirrors the original EA behaviour where profits are reinvested to scale the grid.
- **Volume normalization:** every requested volume is rounded down to the exchange step volume and clamped between the security minimum and maximum volume to avoid order rejections.

## Parameters
- **Take Profit (pips):** distance from the entry price to place the take-profit target for each leg. Defaults to 50 pips.
- **Initial Volume:** base volume per side of the hedge. Defaults to 0.1 contracts.
- **Slow MA / Fast MA:** lengths of the slow and fast SMMA indicators (defaults 20 and 5). The slow period must remain greater than the fast period.
- **Equity DD %:** maximum allowed drawdown from the equity peak before all positions are closed. Default 12%.
- **Multiplier:** martingale factor used for averaging down and for scaling the base volume after major equity growth. Default 1.6.
- **Candle Type:** timeframe of the candles used for calculations. Defaults to 15-minute candles but can be changed to match the chart timeframe of the original EA.

## Notes
- The strategy requires hedging-enabled accounts because it keeps both long and short positions open simultaneously.
- Market orders are used for entries and exits, just like the MQL expert which relied on instant fills. Adapt the order logic if slippage control is needed.
- Ensure that the security metadata (price step, volume step, min/max volume) are correctly configured so that volume normalization works as expected.
