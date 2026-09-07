# Triple Stochastic Timeframes Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram combines formed Stochastic(5,3) momentum from 60-minute, 15-minute, and 5-minute candles. Once per hour it synchronizes the three %K-%D differences and trades a five-minute momentum turn that agrees with both higher timeframes.

![schema](schema.svg)

## Strategy Overview

- Three finished-candle streams provide 60-minute, 15-minute, and 5-minute data and can be built from smaller timeframes.
- Each stream calculates Stochastic %K(5), smooths it with SMA(3) to obtain %D, and subtracts %D from %K.
- The hourly difference samples the latest values from all three streams; Sync releases one complete three-value group on each hourly close.
- The previous synchronized five-minute difference detects a zero-line turn, while the current position prevents adding beyond one unit.
- Both actions are one-unit market operations, and Strategy trades sends every fill to the chart.

## Entry and Exit Rules

- **Long entry**: When the previous entry difference is above zero, the current entry difference is at or below zero, both higher differences are above zero, and the position is not long, buy one unit at market.
- **Short entry**: When the previous entry difference is below zero, the current entry difference is at or above zero, both higher differences are below zero, and the position is not short, sell one unit at market.
- **Exit**: There is no separate exit branch. A valid opposite one-unit action closes an opposing one-unit position to flat; a later valid signal can open the other direction. The diagram has no stop-loss, take-profit, or cooldown.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Higher Candles Series | 01:00:00 | Finished 60-minute candles used for the higher-timeframe calculation and hourly decision pulse. |
| Higher Stochastic %K Length | 5 | Lookback of the higher-timeframe Stochastic %K. |
| Higher Stochastic %K Source | Not set | No alternate indicator input field is selected; higher candles are connected directly. |
| Higher Stochastic %D SMA Length | 3 | Smoothing length applied to higher-timeframe %K to obtain %D. |
| Higher Stochastic %D SMA Source | Not set | No alternate indicator input field is selected; higher %K is connected directly. |
| Middle Candles Series | 00:15:00 | Finished 15-minute candles used for the middle-timeframe calculation. |
| Middle Stochastic %K Length | 5 | Lookback of the middle-timeframe Stochastic %K. |
| Middle Stochastic %K Source | Not set | No alternate indicator input field is selected; middle candles are connected directly. |
| Middle Stochastic %D SMA Length | 3 | Smoothing length applied to middle-timeframe %K to obtain %D. |
| Middle Stochastic %D SMA Source | Not set | No alternate indicator input field is selected; middle %K is connected directly. |
| Entry Candles Series | 00:05:00 | Finished 5-minute candles used for the entry-timeframe calculation and chart. |
| Entry Stochastic %K Length | 5 | Lookback of the entry-timeframe Stochastic %K. |
| Entry Stochastic %K Source | Not set | No alternate indicator input field is selected; entry candles are connected directly. |
| Entry Stochastic %D SMA Length | 3 | Smoothing length applied to entry-timeframe %K to obtain %D. |
| Entry Stochastic %D SMA Source | Not set | No alternate indicator input field is selected; entry %K is connected directly. |
| Order Volume | 1 | Fixed market volume used by both actions. |

## Diagram Details

- Every indicator emits formed values only. SMA(3) receives the corresponding %K output, so each difference is exactly %K minus its three-value average.
- The hourly difference triggers three numeric sample holders before their values enter Sync; the middle and entry holders therefore contribute their latest available readings at that instant.
- Sync clears each completed group and emits three aligned values. The Previous value block stores one synchronized entry difference for the next hourly comparison.
- The position is sampled with the synchronized decision. Position at or below zero permits a buy, and position at or above zero permits a sell, which blocks same-direction accumulation.
- The chart receives five streams: five-minute candles, the three raw %K-%D differences, and all strategy fills.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
