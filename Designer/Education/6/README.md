# Mathematical Cubes and Formulas Schema

This schema file demonstrates the utilization of mathematical cubes and formulas from the "Mathematics" section in the Designer tool, specifically focusing on how to employ these elements in trading strategies.

## Overview

The schema explores the use of formulas to make trading decisions based on the comparison of a security's closing price to its statistical parameters calculated via the Simple Moving Average (SMA) and standard deviation.

## Strategy Details

- **Sell Condition**: The strategy issues a sell order if the closing price of the previous candle is greater than the SMA value of the last 20 periods plus three times the standard deviation of the same period.
- **Buy Condition**: A buy order is executed if the closing price of the previous candle is less than the SMA value of the last 20 periods minus three times the standard deviation.

## Changes in Version 5

- **Mathematics Section**: In version 5 of the Designer software, the "Mathematics" section has been removed. All the cubes previously found under this section have been consolidated into a single "Formula" cube, simplifying the design and implementation process.
- **Position Opening Cube**: The "Open Position" cube has been replaced with the "Register Order" cube in version 5, reflecting changes in how orders are processed within the platform.

This schema effectively showcases how to leverage advanced mathematical calculations to create dynamic and statistically driven trading strategies. The integration of these elements within a trading schema can significantly enhance decision-making processes by grounding them in quantitative analysis.
