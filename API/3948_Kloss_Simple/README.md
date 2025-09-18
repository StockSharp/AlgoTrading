# Kloss Simple Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Kloss Simple Strategy** is a direct conversion of the MetaTrader 4 expert advisor `Kloss_.mq4`. It reconstructs the original trading idea using StockSharp's high-level API and keeps the indicator set identical: an exponential moving average (EMA) calculated on weighted close prices, the Commodity Channel Index (CCI), and the Stochastic oscillator. Signals are generated from the previous completed candle, mirroring the one-bar shift logic in the MQL version. Position sizing can either rely on a fixed order volume or on a risk percentage of the portfolio value, just like the original lot calculation rules.

## Core Idea

1. Monitor the momentum context with **CCI** and **Stochastic** thresholds around their neutral levels.
2. Confirm momentum signals with a short-term **EMA** of the weighted closing price.
3. Enter positions only when the previous candle satisfies all signal conditions, preventing premature trades on incomplete market data.
4. Allow multiple entries in the same direction up to a configurable limit, emulating the "MaxOrders" parameter from the MT4 script.

## Indicator Configuration

- **EMA (MaPeriod)**: Uses the weighted close `(Close * 2 + High + Low) / 4` to match `PRICE_WEIGHTED` from MetaTrader. Acts as a short-term trend filter.
- **CCI (CciPeriod)**: Evaluates momentum deviations from the mean price. Threshold `±CciLevel` defines aggressive versus conservative entries.
- **Stochastic (StochasticKPeriod / DPeriod / Smooth)**: Uses the main %K line to detect overbought or oversold conditions relative to the neutral 50 level. The deviation from 50 is controlled by `StochasticLevel`.

All indicators operate on the primary candle series defined by `CandleType`. The strategy updates indicator values only on finished candles, ensuring stable backtesting and live behaviour.

## Trading Logic

### Long Setup

1. Previous candle close is above the previous EMA value.
2. Previous CCI value is below `-CciLevel`, signalling oversold momentum.
3. Previous Stochastic %K value is below `50 - StochasticLevel`, confirming oversold oscillation.
4. When the conditions hold, any short exposure is closed and a new long position is opened, provided the number of existing long orders is below `MaxOrders`.

### Short Setup

1. Previous candle close is below the previous EMA value.
2. Previous CCI value is above `+CciLevel`, signalling overbought momentum.
3. Previous Stochastic %K value is above `50 + StochasticLevel`, confirming overbought oscillation.
4. When the conditions hold, any long exposure is closed and a new short position is opened, subject to the `MaxOrders` limit.

### Exit Management

- **Stop Loss / Take Profit**: Optional absolute distances in instrument points. If either value is greater than zero, StockSharp's built-in position protection is activated.
- **Opposite Signal**: Before opening in the opposite direction, the current position is closed to mimic the original expert advisor.

## Position Sizing

- **OrderVolume**: Default fixed size that replicates the `Lots` parameter from MT4.
- **RiskPercentage**: When greater than zero, the strategy calculates trade size as a percentage of the portfolio value. It uses instrument margin requirements when available, otherwise falls back to price-based sizing, reproducing the `Lots == 0` behaviour of the MQL code.
- **MaxOrders**: Caps the cumulative volume per direction by allowing up to `MaxOrders * OrderVolume` exposure.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Base order size used when `RiskPercentage` is zero. |
| `MaPeriod` | Length of the EMA built on weighted close prices. |
| `CciPeriod` | Number of bars used in the CCI calculation. |
| `CciLevel` | Absolute CCI threshold for signal generation. |
| `StochasticKPeriod` | Lookback for the Stochastic %K line. |
| `StochasticDPeriod` | Moving average period for the %D line. |
| `StochasticSmooth` | Additional smoothing applied to %K. |
| `StochasticLevel` | Deviation from 50 used for overbought/oversold detection. |
| `MaxOrders` | Maximum number of entries allowed per direction. |
| `StopLossPoints` | Optional stop loss distance in price points. |
| `TakeProfitPoints` | Optional take profit distance in price points. |
| `RiskPercentage` | Portfolio percentage for dynamic position sizing. |
| `CandleType` | Candle series used for all calculations. |

## Practical Notes

- Works best on intraday data where short-term oscillators react quickly to price swings.
- Weighted close price keeps the EMA responsive while still incorporating the high/low range of the candle.
- Because every decision relies on the previous candle, the strategy avoids intra-bar repainting and stays deterministic in historical tests.
- Risk management should be aligned with the broker's contract specifications so that `OrderVolume` and `MaxOrders` correspond to executable trade sizes.
