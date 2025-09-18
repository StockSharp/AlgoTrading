# Blockbuster Bollinger Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Blockbuster Bollinger Breakout strategy is a direct port of the MetaTrader 4 expert advisor "BLOCKBUSTER EA". The original system searched for aggressive reversals after price pushed beyond a Bollinger Band by a configurable distance. This StockSharp version keeps the same logic while embracing the high-level API for candle subscriptions, indicator binding and position management.

## Core Idea

1. Build Bollinger Bands with a user-defined period and deviation.
2. Measure when the close of the current candle breaks above the upper band or below the lower band by an extra offset (in points).
3. Enter short if the close exceeds the upper band plus the offset. Enter long if the close drops below the lower band minus the offset.
4. Manage the position with point-based profit and loss thresholds identical to the MQL settings.

The distance, stop and target are expressed in instrument points. They adapt to the instrument's price step, so a value of `3` means three `PriceStep` units regardless of the underlying symbol.

## Detailed Logic

- **Indicator Calculation**
  - Indicator: Bollinger Bands.
  - Inputs: candle close prices (the MT4 code used `PRICE_OPEN`; this port keeps close prices for better StockSharp compatibility while preserving band length and deviation parameters).
  - Parameters:
    - `BollingerPeriod`: number of candles used in the moving average and standard deviation.
    - `BollingerDeviation`: standard deviation multiplier for the upper and lower bands.
  - Additional offset `DistancePoints` (converted to price using the instrument `PriceStep`).

- **Entry Conditions**
  - **Long**: `Close < LowerBand - Distance` and the current net position is flat or short.
  - **Short**: `Close > UpperBand + Distance` and the current net position is flat or long.
  - Any open opposite position is flattened by the market order size `TradeVolume + |Position|` to mirror the MT4 "One order only" behaviour.

- **Exit Conditions**
  - Positions are monitored on every finished candle. The unrealised profit in points is computed using the instrument `PriceStep`.
  - **Take Profit**: if profit reaches or exceeds `ProfitTargetPoints`.
  - **Stop Loss**: if loss reaches or exceeds `LossLimitPoints`.
  - Exits are performed with market orders that close the entire position.

- **Risk & Money Management**
  - `TradeVolume` sets the base order size. Matching the MetaTrader "Lots" input is as simple as setting the same numeric value.
  - Both stop and target can be disabled by setting the respective parameter to `0`.
  - When both thresholds are enabled, the stop is evaluated after the target, exactly as the original EA checked the profit branch first.

- **State Tracking**
  - The strategy records the entry price at the time of the signal and uses it for all subsequent profit/loss calculations.
  - If an exit order flattens the position, the state is reset automatically.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `BollingerPeriod` | 20 | Number of candles in the Bollinger Bands moving average. |
| `BollingerDeviation` | 2.0 | Standard deviation multiplier. |
| `DistancePoints` | 3 | Extra distance beyond the band before a trade is placed (instrument points). |
| `ProfitTargetPoints` | 3 | Take-profit threshold in instrument points. Set to 0 to disable. |
| `LossLimitPoints` | 20 | Stop-loss threshold in instrument points. Set to 0 to disable. |
| `TradeVolume` | 1 | Volume for new entries. |
| `CandleType` | 1-minute time frame | Candle type used for calculations. |

## Usage Notes

- Works on any instrument that supplies candles and a non-zero `PriceStep`. Forex pairs, index CFDs and liquid futures mirror the original EA environment best.
- Because the indicator now relies on closing prices, testing on the intended time frame is recommended to ensure behaviour similar to the MT4 version.
- The strategy uses `CreateChartArea` helpers to visualise candles, the Bollinger Bands and executed trades when a chart is available in the UI.
- The logic assumes continuous evaluation on finished candles, ensuring deterministic behaviour in backtesting and live trading.

## Tags

- Category: Counter-trend breakout
- Direction: Both
- Indicators: Bollinger Bands
- Stops: Yes (configurable)
- Timeframe: Short-term (default 1 minute)
- Complexity: Simple

