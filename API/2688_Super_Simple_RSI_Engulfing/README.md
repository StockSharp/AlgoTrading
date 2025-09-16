# Super Simple RSI Engulfing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the original SSEATwRSI MetaTrader expert advisor in StockSharp. It monitors finished candles and calculates a 7-period RSI on the candle high. A trade is triggered only when the RSI reaches an extreme and the previous two bars form a clean engulfing reversal.

A long setup requires the RSI to move above the overbought threshold while a bearish candle is fully engulfed by the next bullish candle. A short setup mirrors this logic using an oversold RSI reading and a bullish-to-bearish engulfing pattern. Position size is fixed by the `Volume` parameter, but any opposite exposure is flattened before opening a new trade.

Once in the market, the strategy keeps watching the global profit and loss. If floating PnL reaches the configured profit goal (in account currency) or drops below the allowed loss, it closes the entire position. There are no additional trailing stops; trades are managed solely by the pattern reversal and the account-level thresholds.

## Details

- **Entry Criteria**:
  - **Long**: RSI on highs > `OverboughtLevel` and the last candle engulfs a bearish bar from two bars ago while price closes above that older open.
  - **Short**: RSI on highs < `OversoldLevel` and the last candle engulfs a bullish bar from two bars ago while price closes below that older open.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Account PnL ≥ `ProfitGoal` → flatten.
  - Account PnL ≤ `-MaxLoss` → flatten.
  - Opposite signal automatically offsets the previous position when a new order is placed.
- **Stops**: Currency-based take-profit and max-loss checks derived from total strategy PnL.
- **Filters**:
  - RSI calculated on the candle high to emphasise exhaustion moves.
  - Confirmation via a two-bar engulfing reversal.

## Parameters

- `Volume` = 0.1 – Order size in contracts. Existing exposure is offset before opening a new trade.
- `ProfitGoal` = 190 – Currency profit target that forces a flat position once reached.
- `MaxLoss` = 10 – Maximum allowed currency loss before the strategy closes all positions. The check uses `-MaxLoss` internally.
- `RsiPeriod` = 7 – Averaging length of the RSI indicator.
- `RsiPrice` = High – Price source used for the RSI calculation.
- `OverboughtLevel` = 88 – RSI level that must be exceeded before taking a long reversal.
- `OversoldLevel` = 37 – RSI level that must be undershot before taking a short reversal.
- `CandleType` = 1-hour candles by default; adjust to match the timeframe of the original chart.
