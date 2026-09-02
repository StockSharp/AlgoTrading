# KDJ Expert Advisor Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A port of the MetaTrader KDJ advisor. The J line is rebuilt here as the difference between the %K and %D lines of the Stochastic Oscillator, and that difference decides the side: the diagram buys when it turns positive or when %K keeps rising while it is already positive, and sells on the mirror conditions. Two things are adapted to the packaged history: the original four-hour candles become hourly ones, so a month of data still gives enough bars, and the pip-based stop and target become percent distances that work on any instrument.

![schema](schema.svg)

## Strategy Overview

- The Stochastic Oscillator with a 30-bar %K and a 6-bar %D stands in for KDJ, and the difference K - D plays the role of the J line.
- There are two ways into a trade: the difference crossing zero, or the %K line moving in the direction the sign of the difference already points to.
- A position is opened only from flat, so the strategy never pyramids and never reverses; the protection block is what ends the trade.

## Entry and Exit Rules

- **Long entry**: K - D is positive and either it was negative on the previous candle, which makes this candle the zero cross, or %K is higher than on the previous candle. The position must be flat; one lot is bought at market.
- **Short entry**: K - D is negative and either it was positive on the previous candle, which makes this candle the zero cross, or %K is lower than on the previous candle. The position must be flat; one lot is sold at market.
- **Exit**: There is no exit signal at all, exactly as in the original: the position protection block closes the trade with market orders at a 2% take profit or a 1% stop loss, which is the percent equivalent of the 450 and 250 pip distances of the code.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| %K Length (KDJ period) | 30 | Length of the %K line, the KDJ period of the original advisor. |
| %D Smoothing | 6 | Smoothing length of the %D line. |
| Take profit, % | 2 | Take profit distance, in percent of the entry price. |
| Stop loss, % | 1 | Stop loss distance, in percent of the entry price. |
| Volume | 1 | Order volume, in lots. |
| Candles | 01:00:00 | Candle time frame the whole diagram works on; the original used four hours. |

## Diagram Details

- Two converter blocks split the Stochastic Oscillator into its %K and %D lines, and a formula block subtracts one from the other.
- Previous-value blocks hold K - D and %K one candle back, which is how the zero cross and the slope are recognised without a crossing block.
- Four logical AND blocks build the two ways into each direction and already carry the flat-position flag; an OR merges the pair into one trigger per side.
- Both entry blocks pass their own trades to the position protection block, so every fill immediately receives a stop and a target.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
