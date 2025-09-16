# Jupiter M Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Grid-based strategy translated from the MetaTrader expert "Jupiter M. 4.1.1".
The algorithm builds a basket of orders using a configurable step and adapts
both take profit and volume as new levels are opened.

## Details

- **Entry Criteria**:
  - Long: price drops by the step size and (optional) CCI < -100
  - Short: price rises by the step size and (optional) CCI > 100
- **Long/Short**: Both
- **Exit Criteria**: Basket reaches the calculated take profit
- **Stops**: Breakeven after a specified number of steps
- **Default Values**:
  - `TakeProfit` = 10
  - `FirstStep` = 20
  - `FirstVolume` = 0.01
  - `VolumeMultiplier` = 2
  - `CciPeriod` = 50
  - `CandleType` = 5 minute candles
- **Filters**:
  - Category: Grid, mean reversion
  - Direction: Both
  - Indicators: CCI (optional)
  - Stops: Breakeven
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High

## Parameters

- `TakeProfit` – profit target in price units for the basket.
- `UseAverageTakeProfit` – calculate take profit from average price of open orders.
- `DynamicTakeProfit` – reduce take profit after `TpDynamicStep` using `TpDecreaseFactor` with a floor at `MinTakeProfit`.
- `BreakevenClose` / `BreakevenStep` – move target to breakeven after a number of steps.
- `FirstStep` – initial distance between grid levels.
- `DynamicStep`, `StepIncreaseStep`, `StepIncreaseFactor` – increase step for each additional order.
- `MaxStepsBuy` / `MaxStepsSell` – maximum number of orders per direction.
- `FirstVolume`, `VolumeMultiplier`, `MultiplyUseStep` – control volume growth in the grid.
- `CciFilter` / `CciPeriod` – optional Commodity Channel Index filter for first order.
- `AllowBuy` / `AllowSell` – enable trading directions.
- `CandleType` – candle timeframe for calculations.

The strategy aims to capture price mean reversion by averaging into positions
and closing the basket at dynamic profit targets.
