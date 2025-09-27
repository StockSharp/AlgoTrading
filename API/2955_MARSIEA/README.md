# MA RSI EA Strategy

## Overview
The **MA RSI EA Strategy** reproduces the logic of the original MetaTrader expert advisor that combines a fast moving average with a short-period RSI filter. The strategy trades on the selected candle series, evaluates new orders only on finished bars, and uses dynamic position sizing based on account balance or equity. When the floating profit of all open positions becomes positive, every position is closed immediately to lock in the gain.

## Indicators
- **Moving Average** – configurable method (simple, exponential, smoothed, linear weighted) with price source selection and optional shift.
- **Relative Strength Index (RSI)** – short-term oscillator that reads from the same candle price family as in the MQL version.

## Trading Logic
1. For every completed candle the strategy calculates the moving average and RSI values using the configured price sources.
2. The most recent moving average value can be shifted by a user-defined number of bars to match the MQL behaviour.
3. It evaluates the floating PnL of the current net position:
   - If the floating result of all open positions is **greater than zero**, the strategy closes the entire position to realise the profit.
   - If the floating result is **negative**, the side with the smaller loss (buy-side vs. sell-side) is reinforced by opening an additional trade in that direction.
4. If there is no averaging signal, the RSI + MA filter is applied:
   - **Short entry** – RSI ≥ `RsiOverbought` and the candle open price is below the shifted moving average.
   - **Long entry** – RSI ≤ `RsiOversold` and the candle open price is above the shifted moving average.

## Exit Logic
- Positive floating PnL triggers `CloseAllPositions`, flattening the strategy immediately.
- Manual reversal signals from the averaging logic close the opposite exposure because StockSharp works with net positions.

## Position Sizing
`LotSizingModes` mirrors the `OptLot` selection from the EA:
- **Fixed** – always send `LotSize` volume.
- **Balance** – convert `PercentOfBalance` of the portfolio value into volume using the candle close price.
- **Equity** – convert `PercentOfEquity` of the current portfolio equity into volume.

The calculated volume is rounded to the nearest `Security.VolumeStep` (when available) so that orders comply with the instrument’s lot size.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `LotOption` | Volume calculation mode (`Fixed`, `Balance`, `Equity`). | `Balance` |
| `LotSize` | Fixed lot value for the `Fixed` mode. | `0.01` |
| `PercentOfBalance` | Percentage of balance used in `Balance` mode. | `2` |
| `PercentOfEquity` | Percentage of equity used in `Equity` mode. | `3` |
| `FastMaPeriod` | Moving average length. | `4` |
| `FastMaShift` | Shift applied to the moving average result. | `0` |
| `FastMaMethod` | Moving average calculation method (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `LinearWeighted` |
| `FastMaPrice` | Candle price source for the moving average. | `Open` |
| `RsiPeriod` | RSI length. | `4` |
| `RsiPrice` | Candle price source for the RSI. | `Open` |
| `RsiOverbought` | RSI level that defines an overbought market. | `80` |
| `RsiOversold` | RSI level that defines an oversold market. | `20` |
| `CandleType` | Candle series used by the strategy. | `15-minute time frame` |

## Candle Price Sources
`CandlePriceSources` replicates the MQL applied price list:
- `Open`, `High`, `Low`, `Close`
- `Median` = (High + Low) / 2
- `Typical` = (High + Low + Close) / 3
- `Weighted` = (High + Low + Close + Close) / 4

## Notes
- Orders are generated only when the strategy is online and the candle is finished, matching the original EA that triggers on new bars.
- Because StockSharp maintains a net position, averaging signals automatically reduce or flip the current exposure instead of creating hedge positions.
- Python implementation is intentionally omitted as requested.
