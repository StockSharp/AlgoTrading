# TrendMeLeaveMe Pending Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp implementation recreates the original MetaTrader "TrendMeLeaveMe" expert advisor. The idea is to manually follow a dynamic trend channel and use pending stop orders to catch breakouts whenever price hugs the trend line. Because StockSharp does not work with user-drawn chart objects, the strategy rebuilds the channel center automatically with a linear regression indicator and then reproduces the same offset logic that the MQL version applied to the upper and lower guide lines.

The approach is designed for both long and short entries. Once a stop order is triggered the position is immediately protected with static stop-loss and take-profit orders that mirror the distances configured in the EA. Pending orders are constantly refreshed so that the activation levels track the latest regression line value.

## How the strategy works

1. A candle subscription drives a `LinearRegression` indicator that acts as the middle trend line.
2. The user defines four offsets (upper/lower for buy and sell scenarios) in instrument price steps. The strategy translates them into prices above or below the regression line.
3. When the last candle closes between the trend line and the configured lower offset, a buy stop is positioned at the upper offset. Symmetrically, when price closes between the line and the upper offset, a sell stop is placed at the lower boundary.
4. If the market drifts outside those activation zones, the corresponding pending order is cancelled so the strategy does not clutter the book.
5. After a stop order executes, the trade is wrapped with a static stop loss and take profit that use the same point distances as the original expert advisor.

## Signals

- **Buy setup**: Candle close is below or equal to the regression line but still above the buy lower offset. A buy stop order is placed at the upper offset and follows the line while the condition remains valid.
- **Sell setup**: Candle close is above or equal to the regression line but still below the sell upper offset. A sell stop order is placed at the lower offset and trails the trend line.
- **No setup**: When price is outside the activation corridor, existing pending orders are removed.

## Risk management

- Buy trades use `BuyStopLossSteps` and `BuyTakeProfitSteps` to calculate fixed stop-loss and take-profit levels from the entry price.
- Sell trades use `SellStopLossSteps` and `SellTakeProfitSteps` for the same purpose.
- Protective orders are recalculated only when the net position flips, mimicking how MetaTrader attaches stop levels directly to each pending order.

## Parameters

- `CandleType` – candle aggregation used for calculating the trend line.
- `TrendLength` – number of candles in the linear regression window.
- `BuyStepUpper` / `BuyStepLower` – offsets (in price steps) defining the upper trigger and lower activation threshold for long setups.
- `SellStepUpper` / `SellStepLower` – offsets (in price steps) defining the activation corridor for short setups.
- `BuyTakeProfitSteps` / `BuyStopLossSteps` – distances for long position exits, expressed in price steps.
- `SellTakeProfitSteps` / `SellStopLossSteps` – distances for short position exits.
- `BuyVolume` / `SellVolume` – volume used for pending orders on each side.

## Notes

- Because there are no manual trend lines, the regression indicator replaces the chart objects from the MQL strategy. Users can experiment with the regression length to approximate their manual trend analysis.
- The strategy only trades when the exchange connection is live (`IsFormedAndOnlineAndAllowTrading`).
- Pending orders are automatically cancelled whenever a position in the same direction already exists, reproducing the single-order behaviour of the original EA.
