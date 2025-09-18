# FiboChannel Line Strategy

## Overview
The **FiboChannel Line Strategy** is a conversion of the MetaTrader expert advisor “FIBOCHANNEL”.
The original robot relied on the direction of a manually drawn Fibonacci channel, momentum swings on a
higher timeframe, and a combination of linear weighted moving averages and MACD signals. The StockSharp
port keeps the same spirit while leveraging high-level indicator bindings and built-in risk management.

Key ideas:

- Follow the dominant trend using a pair of linear weighted moving averages (LWMA).
- Confirm momentum spikes around the neutral level of the Momentum oscillator.
- Filter trades with the MACD line versus signal line relationship.
- Check the slope of a linear regression channel instead of reading chart objects.
- Manage positions via automatic percentage based protection.

The strategy works on any instrument that supports candle aggregation. The default timeframe is 30-minute
candles, which provides a balance between responsiveness and indicator stability.

## Trading Logic
1. **Trend filter** – when the fast LWMA closes above the slow LWMA the market is considered bullish and
   only long trades are evaluated. When it is below, only shorts are considered.
2. **Momentum requirement** – a rolling window of three most recent Momentum readings must show that at
   least one value deviated from the neutral level 100 by the configured threshold. This replicates the
   multi-bar momentum strength checks from the MQL version.
3. **MACD filter** – longs require the MACD line to be above the signal line, shorts require the opposite.
4. **Channel direction** – the linear regression slope must be positive (for longs) or negative (for shorts)
   beyond the `Slope Threshold`. This mimics the ascending/descending channel validation from the original
   expert that compared anchor points of a Fibonacci channel object.
5. **Entries and reversals** – if all conditions align and there is no existing position in that direction,
   the strategy cancels active orders and sends a market order with size `Volume + |Position|`. This allows
   smooth reversals.
6. **Exits** – if the channel direction or MACD filter stops supporting the open trade, the position is
   closed after canceling resting orders. Additionally, protective stop-loss, take-profit, and max drawdown
   rules are configured through `StartProtection`.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Candle Type` | Candle aggregation used for all indicators. | 30-minute time frame |
| `Fast LWMA` | Length of the fast linear weighted moving average. | 6 |
| `Slow LWMA` | Length of the slow linear weighted moving average. | 85 |
| `Momentum Period` | Number of bars for the Momentum indicator. | 14 |
| `Momentum Threshold` | Minimal absolute deviation from 100 required inside the momentum buffer. | 0.3 |
| `Channel Length` | Bars used when computing the linear regression slope. | 50 |
| `Slope Threshold` | Minimal absolute slope value to confirm trend direction. | 0.0 |
| `MACD Fast` | Fast EMA period within the MACD calculation. | 12 |
| `MACD Slow` | Slow EMA period within the MACD calculation. | 26 |
| `MACD Signal` | Signal line period of MACD. | 9 |
| `Take Profit %` | Distance of the protective take-profit in percent. | 2 |
| `Stop Loss %` | Distance of the protective stop-loss in percent. | 1 |
| `Equity Risk %` | Maximum account equity drawdown allowed before flattening all positions. | 3 |

All numeric parameters expose optimization hints that mirror the typical ranges of the MQL inputs.

## Risk Management
`StartProtection` is configured to apply:

- Percentage based stop-loss and take-profit relative to the entry price.
- Equity drawdown guard that flattens the strategy if the loss exceeds the configured percentage.

These protections substitute the numerous balance, trailing, and break-even routines from the original
expert while providing clearer and safer behaviour inside StockSharp.

## Differences from the MetaTrader Version
- Chart object reads were replaced with a regression slope filter because StockSharp strategies do not
  interact with manual Fibonacci channels.
- Instead of a mix of money-based trailing logic the strategy relies on `StartProtection`.
- The indicator stack remains the same (LWMA, Momentum, MACD), but it is implemented using high-level
  bindings and without direct indicator value polling.
- Alerts, emails, and push notifications were removed as the StockSharp environment already provides
  consolidated logging.

## Usage Notes
1. Attach the strategy to a portfolio and security, configure the lot size through the `Volume` property,
   and adjust the parameters as needed.
2. Ensure historical data is available for the selected candle type so that the momentum buffer and slope
   indicator can form properly.
3. Run in paper trading first to fine tune the momentum threshold and risk parameters according to the
   traded instrument’s volatility.
