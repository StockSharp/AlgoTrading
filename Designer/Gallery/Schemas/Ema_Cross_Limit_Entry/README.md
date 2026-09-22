# EMA Cross Chasing Limit Entry Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram keeps the EMA(12)/EMA(26) crossover and Momentum(10) confirmation from Franks4HourLimitOrdersStrategy, but makes execution explicit: a limit starts at the signal close, follows later closes through Order replacing while the side remains valid, and is cancelled on the opposite cross.

![schema](schema.svg)

## Strategy Overview

- Finished candles feed two exponential moving averages and Momentum; Crossing emits only on a genuine change of EMA order.
- A bullish cross requires positive Momentum and Position <= 0; a bearish cross requires negative Momentum and Position >= 0.
- Order registering places the first limit at the signal candle close with price-step shrinking disabled.
- Combination retains the newest order returned by each Order replacing operation, so later updates and cancellation always target the live object.
- Replacement is gated by the continuing EMA and Momentum side; it stops when confirmation disappears instead of churning unconditionally.

## Entry and Exit Rules

- **Long entry**: EMA(12) crosses above EMA(26), Momentum is positive, and Position is flat or short. A buy limit at the close uses abs(Position)+1 volume, combining the source's close-short and open-long market orders into one net limit reversal.
- **Short entry**: EMA(12) crosses below EMA(26), Momentum is negative, and Position is flat or long. A sell limit at the close uses the same net-reversal sizing.
- **Exit**: An opposite EMA cross cancels the pending order and may submit the opposite one. A filled position is also protected by the diagram-only 1% stop and 3% target; a protection fill cancels any remaining pending reference.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Finished-candle interval; the gallery replay uses five minutes while the C# default is four hours. |
| Fast EMA Length | 12 | Number of values in the fast ExponentialMovingAverage. |
| Slow EMA Length | 26 | Number of values in the slow ExponentialMovingAverage. |
| Momentum Length | 10 | Number of values in Momentum, whose sign confirms the crossover. |

## Diagram Details

- The C# source uses market orders and has no pending-order management. Limit registration, replacement, and cancellation are the deliberate execution adaptation demonstrated here.
- Replacement moves the unfilled limit to each new candle close only while fast/slow EMA order, Momentum sign, and position side still permit that setup.
- The source default is four-hour candles. This example defaults to five minutes because the bundled one-month history barely forms EMA(26) on H4; Candle Time Frame remains exposed so 04:00:00 can be restored.
- Base volume 1 and Position protection at stop 1% / take 3% are fixed diagram additions, not constructor parameters of Franks4HourLimitOrdersStrategy.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
