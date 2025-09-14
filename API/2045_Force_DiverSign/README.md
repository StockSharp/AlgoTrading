# Force DiverSign Strategy

The Force DiverSign strategy trades based on divergence signals between two Force Index indicators computed with different smoothing periods.
It looks for a three-candle reversal pattern together with opposite swings in the fast and slow Force values. When a bullish divergence appears,
the strategy buys; when a bearish divergence appears, it sells.

## Parameters
- `Period1` – period for the fast Force Index.
- `Period2` – period for the slow Force Index.
- `MaType1` – moving average type used to smooth the fast Force Index.
- `MaType2` – moving average type used to smooth the slow Force Index.
- `CandleType` – timeframe of candles for calculations.

## Trading Logic
1. Calculate raw Force Index as the volume multiplied by the change of close price.
2. Smooth the raw value with two moving averages to obtain fast and slow Force series.
3. Store the last five Force values and the last four candles.
4. **Buy** when:
   - Three previous candles form a down–up–down pattern.
   - Both Force series form a local bottom then rise.
   - Fast and slow Force move in opposite directions between the first and third candle.
5. **Sell** when:
   - Three previous candles form an up–down–up pattern.
   - Both Force series form a local top then fall.
   - Fast and slow Force move in opposite directions between the first and third candle.

Positions are reversed on each signal: a buy closes an existing short and a sell closes a long.
