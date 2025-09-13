# Unseasonalised ATR and Forecast Strategy

## Overview

This strategy analyzes the average trading range of recent candles and forecasts the next range using linear trend regression. It does not place any trades but displays statistics that can be used for manual decisions.

## Parameters

- **SampleSize** – number of recent candles used for calculations.
- **DesiredRange** – target range used for confidence interval estimation.
- **CandleType** – candle series to analyze.

## Indicators

- SimpleMovingAverage – used to compute the average range.
- StandardDeviation – measures volatility of the range.
- Linear regression (custom) – forecasts the next range and MAPE.

## Behavior

For each finished candle the strategy:

1. Calculates the range (high minus low) and updates average and standard deviation.
2. Estimates a confidence interval for the desired range.
3. Builds a linear trend of the ranges and forecasts the next one.
4. Evaluates the mean absolute percentage error (MAPE) of the forecast.

Values are logged to the strategy output and can be visualized on the chart.

## Notes

- The strategy is informational and does not execute orders.
- Ranges are measured in price units; adapt the `DesiredRange` parameter to your instrument.
