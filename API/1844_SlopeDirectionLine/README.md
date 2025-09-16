# Slope Direction Line Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the behavior of the *Slope Direction Line* Expert Advisor. It analyzes the slope of a linear regression line built on closing prices. A long position is opened when the regression slope turns positive after being negative, while a short position is opened when it turns negative after being positive. Opposite positions are closed on every change of direction. Optional stop-loss and take-profit percentages protect positions via the built-in `StartProtection` mechanism.

## Details
- **Indicator** – `LinearRegression` from StockSharp. The strategy uses the `LinearRegSlope` component as the signal.
- **Signal** – cross of the slope through zero. A positive slope indicates an uptrend; a negative slope signals a downtrend.
- **Entry/Exit** – when the slope changes sign the current position is closed and, if allowed, a new position in the direction of the slope is opened.
- **Risk control** – `StartProtection` is configured with user-defined take-profit and stop-loss percentages.

## Parameters
| Name | Description |
|------|-------------|
| `CandleType` | Time frame used for building candles. |
| `Length` | Number of bars used in the linear regression calculation. |
| `TakeProfitPercent` | Percent distance to take profit from entry price. |
| `StopLossPercent` | Percent distance to stop loss from entry price. |
| `AllowLong` | Permit opening long positions. |
| `AllowShort` | Permit opening short positions. |

## Usage
1. Add the strategy to a StockSharp application.
2. Configure the parameters according to the desired time frame and risk.
3. Start the strategy and monitor trades on the chart.

