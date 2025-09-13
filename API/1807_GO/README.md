# GO Strategy

This strategy calculates a composite **GO** value based on exponential moving averages (EMA) of open, high, low, and close prices multiplied by volume. Trading decisions are made according to the sign and level of the GO value.

## Formula

`GO = ((C - O) + (H - O) + (L - O) + (C - L) + (C - H)) * V`

Where:
- `C`, `O`, `H`, `L` – EMA values of Close, Open, High, and Low prices.
- `V` – volume of the processed candle.

## Trading Rules

- **Open Long**: GO > `OpenLevel`
- **Open Short**: GO < `-OpenLevel`
- **Close Long**: GO < (`OpenLevel` - `CloseLevelDiff`)
- **Close Short**: GO > -(`OpenLevel` - `CloseLevelDiff`)

## Parameters

| Name | Description |
|------|-------------|
| `MaPeriod` | EMA period for price smoothing. |
| `OpenLevel` | GO level to trigger new positions. |
| `CloseLevelDiff` | Difference between open and close levels. |
| `ShowGo` | Whether to log GO values. |
| `CandleType` | Type of candles used for processing. |

The strategy operates on finished candles and uses market orders for position management.
