# TradingLab Best MACD Strategy

The **TradingLab Best MACD** strategy ports the MetaTrader expert advisor from Mueller Peter into StockSharp's high-level API. It blends momentum signals from the MACD with structure confirmation from recent support/resistance touches and a 200-period EMA trend filter. Orders are executed on closed candles only and every trade receives a dynamic stop loss and take profit derived from the EMA distance, mirroring the original MQL risk model.

## Core logic

1. **Indicator stack**
   - 200-period EMA on the selected candle type acts as a directional filter.
   - Standard MACD (12/26 with 9-signal) generates bullish and bearish crossover impulses.
   - Two channel indicators track the most recent structure:
     - `Highest` with length 20 models custom resistance.
     - `Lowest` with length 10 models custom support.
2. **Signal persistence counters**
   - When price pierces the resistance (high of the previous candle above the stored level) a `ResistanceTouch` counter is reset to `SignalValidity` and decremented on each subsequent bar.
   - A symmetrical `SupportTouch` counter tracks lows below the support band.
   - MACD crossovers below zero (for longs) or above zero (for shorts) reset the corresponding MACD counters, maintaining the momentum bias for several candles.
3. **Entry rules**
   - **Long**: close above the EMA, both MACD-up and support-touch counters active, and at least one of them triggered on the latest candle. Volume equals the configured `OrderVolume` plus any contracts required to flip from a short position.
   - **Short**: close below the EMA, both MACD-down and resistance-touch counters active, with freshness identical to the long setup.
4. **Exit management**
   - Stops and targets are recalculated on entry. The stop is positioned `StopDistancePoints` away from the EMA (converted to price using `PriceStep`). The take profit mirrors the MQL formula: distance between entry and EMA plus the stop offset, multiplied by 1.5.
   - Each finished candle checks whether the low/high breaches these levels and closes the position if triggered.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `SignalValidity` | Number of finished candles for which structure or MACD signals remain valid. | 7 |
| `OrderVolume` | Market order size used for entries. | 1 |
| `StopDistancePoints` | Offset from the EMA (in instrument points) for the protective stop. | 50 |
| `CandleType` | Primary timeframe feeding the indicators (defaults to 5-minute candles). | `TimeSpan.FromMinutes(5).TimeFrame()` |

All parameters are exposed through `StrategyParam` so they can be optimized inside the designer.

## Trading workflow

1. Subscribe to the configured candle source and bind EMA, MACD, Highest and Lowest indicators.
2. Maintain previous candle values to emulate the original `CopyBuffer` lookback behaviour from MQL.
3. Update the four signal counters every closed candle.
4. Validate trading conditions with `IsFormedAndOnlineAndAllowTrading()` before submitting new orders.
5. Execute market orders with dynamically calculated exit levels and monitor them on each subsequent candle.

## Notes

- This conversion focuses on the C# implementation only; no Python port or `PY/` folder is included, as requested.
- The strategy relies solely on high-level API components (`SubscribeCandles`, `BindEx`, `BuyMarket`, `SellMarket`) and avoids manual indicator buffers.
- Charting helpers draw the candles, EMA and MACD automatically when a chart area is available, simplifying visual validation.
