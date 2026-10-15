# Percent Grid Ladder Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram builds a symmetric percentage grid around the first finished five-minute close. Three pending buy limits sit below the anchor and three sell limits above it; after the first fill, the remaining ladder is cancelled and the resulting position is managed by percentage protection.

![schema](schema.svg)

## Strategy Overview

- The first finished five-minute candle latches its close as the grid anchor; no Level1 quote or bid/ask midpoint is used.
- Grid spacing of 1.5% creates up to three buy levels below the anchor and three sell levels above it.
- Grid Levels per Side enables rungs one through three, while the long and short switches gate the two sides independently.
- The first entry fill cancels every still-working grid order and starts 2% take-profit and 3% stop-loss protection.
- A protective exit cancels any residual orders, latches the latest close as a new anchor, and registers a fresh ladder.

## Entry and Exit Rules

- **Long entry**: When long trading is enabled, one-unit buy limits are placed at anchor × (1 − spacing × rung). One, two, or three lower rungs are active according to Grid Levels per Side.
- **Short entry**: When short trading is enabled, one-unit sell limits are placed at anchor × (1 + spacing × rung). One, two, or three upper rungs are active according to Grid Levels per Side.
- **Exit**: The first filled order becomes the sole active position cycle. Position protection closes it at +2% or −3%; that exit reanchors the next six-order grid at the latest finished close.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Grid Spacing, % | 1.5 | Percentage distance between adjacent grid rungs; 1.5 means 1.5%. |
| Grid Levels per Side | 3 | Number of enabled rungs on each side, from one through the diagram maximum of three. |
| Enable Long | true | Enables registration of buy limits below the anchor. |
| Enable Short | true | Enables registration of sell limits above the anchor. |
| Take Profit, % | 2 | Profit distance from the filled entry used by Position protection. |
| Stop Loss, % | 3 | Loss distance from the filled entry used by Position protection. |

## Diagram Details

- The C# strategy keeps virtual levels and submits market orders when a candle close reaches them. The diagram intentionally materializes those levels as real pending limit orders so the order cubes and cancellation lifecycle are visible.
- The source can trigger several levels over time. This diagram uses a deliberate one-position-cycle simplification: one fill cancels all other rungs, trades a fixed volume of one, and waits for protection before rebuilding.
- Grid Levels per Side supports values from one through three. The diagram has a visual maximum of three rungs, so values above three cannot create additional blocks.
- Reanchoring is implemented as cancellation followed by fresh registration; no order-replacement block is used. Price shrinking is disabled for replay instruments without a declared price step.
- The exact candle close is both the initial and subsequent anchor, matching the source's reset price and avoiding an unnecessary Level1 dependency.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
