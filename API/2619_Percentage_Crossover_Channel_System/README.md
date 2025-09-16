# Percentage Crossover Channel System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a direct port of the MetaTrader expert advisor *Exp_PercentageCrossoverChannel_System*. It tracks how price interacts with a custom "Percentage Crossover Channel" and reacts when candles move back inside the channel after previously breaking out. The code was rewritten with StockSharp high-level APIs and preserves the original signal flow.

## Trading Logic

1. **Indicator construction**
   - The Percentage Crossover Channel builds an adaptive midline that stays close to price but cannot drift away faster than a fixed percentage (`Percent`).
   - Upper and lower bands are derived from the midline using the same percentage distance.
   - Every completed candle is coloured according to its relationship with the channel from `Shift` bars ago:
     - Color `3` / `4`: close above the upper band (bearish/bullish candle body respectively).
     - Color `0` / `1`: close below the lower band (bearish/bullish body respectively).
     - Color `2`: candle finished inside the channel.

2. **Entry and exit rules**
   - Evaluate the last `SignalBar` candle and the one immediately preceding it (mirrors the MQL `CopyBuffer` call).
   - **Bullish sequence** (`olderColor > 2`): the market recently closed above the channel. If the most recent candle moved back inside (`recentColor < 3`) the strategy:
     - Closes any active short if `SellPositionsClose` is enabled.
     - Opens a long position when no trades are open and `BuyPositionsOpen` is enabled.
   - **Bearish sequence** (`olderColor < 2`): the market recently closed below the channel. If the latest candle returned inside (`recentColor > 1`) the strategy:
     - Closes any long if `BuyPositionsClose` is enabled.
     - Opens a short when no trades are active and `SellPositionsOpen` is enabled.
   - The logic therefore waits for a breakout followed by a re-entry into the channel before committing in the breakout direction.

3. **Risk management**
   - Optional stop loss and take profit are expressed in price steps and evaluated on candle highs/lows.
   - If a protective order is triggered the strategy leaves the market and ignores fresh entries for the same bar, mimicking the MQL behaviour where broker-side stops close the trade first.

## Parameters

| Name | Description |
| ---- | ----------- |
| `Percent` | Channel width in percent. Matches the MQL indicator input. |
| `Shift` | Number of bars used to compare the breakout with historical bands. |
| `SignalBar` | Offset (in bars) for signal evaluation. A value of 1 means "previous bar" like the original EA default. |
| `BuyPositionsOpen` / `SellPositionsOpen` | Enable or disable opening trades in the corresponding direction. |
| `BuyPositionsClose` / `SellPositionsClose` | Enable or disable force-closing opposite positions on a new signal. |
| `StopLoss` | Stop loss distance expressed in multiples of `Security.PriceStep`. Set to zero to disable. |
| `TakeProfit` | Take-profit distance in price steps. Set to zero to disable. |
| `CandleType` | Timeframe for candle subscription. Defaults to four-hour bars to mirror `PERIOD_H4`. |

## Implementation Notes

- The indicator logic is implemented inline because StockSharp does not provide a native Percentage Crossover Channel. The midline calculations, band derivation and colour assignments reproduce the MQL source algorithm step by step.
- Position management follows the original helper functions (`BuyPositionOpen`, `SellPositionOpen`, etc.) by closing opposing trades before opening a new one and by skipping entries when an opposite position is still present.
- Money management, deviation handling and margin-mode specific lot sizing from the original include file are not replicated. StockSharp users should configure the strategy volume via standard `Strategy` properties or the hosting environment.
- Stop loss / take profit values are interpreted as *price steps* because MetaTrader inputs are specified in points. Ensure that the connected security exposes a valid `PriceStep`.

## Usage Tips

- Attach the strategy to an instrument with reliable four-hour data if you want behaviour identical to MetaTrader. Adjust `CandleType` to experiment with intraday operation.
- Because the entry logic requires two completed candles with valid colour information, allow the strategy to warm up with at least `Shift + SignalBar + 1` bars of history.
- The channel is sensitive to the `Percent` input. Smaller values hug price tightly and increase trading frequency, whereas larger values focus on stronger breakouts.
- When combining with portfolio-level risk controls, keep in mind that this implementation opens at most one position at a time and flips between long, flat or short states.

