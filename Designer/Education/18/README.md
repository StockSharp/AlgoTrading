# Pair Trading Strategy Schema
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This schema presents a pair trading strategy based on the relative value of two securities. It incorporates a unique approach to identify and capitalize on price discrepancies between two correlated assets.

## Overview

Pair trading is a market-neutral strategy that involves buying one asset and simultaneously selling another when their price ratio deviates from the historical norm. This schema uses the example of two specific instruments: BTCUSDT@BNBFT and TONUSDT@BNBFT.

## Strategy Logic

- **Index Calculation**: The strategy calculates an index based on the formula `BTCUSDT@BNBFT / TONUSDT@BNBFT`. This index helps determine the relative strength or weakness of one instrument compared to the other.
- **Buy Condition**: If the index rises, indicating that BTCUSDT@BNBFT is becoming more expensive relative to TONUSDT@BNBFT, the strategy buys the cheaper asset (TONUSDT@BNBFT) and sells the more expensive one (BTCUSDT@BNBFT).
- **Sell Condition**: If the index falls, suggesting that BTCUSDT@BNBFT is getting cheaper relative to TONUSDT@BNBFT, the strategy buys the more expensive asset (BTCUSDT@BNBFT) and sells the cheaper one (TONUSDT@BNBFT).

## Key Features

- **Rounded Values**: Utilizes the `round` operator to convert calculated index values to integers. This simplification aids in decision-making by providing cleaner, more actionable signals.
- **Market Neutrality**: Aims to profit from the convergence of the price ratio towards its historical average, regardless of the market's overall direction.

## Application and Benefits

- **Risk Mitigation**: By trading in pairs that are historically correlated, the strategy minimizes the market risk, as gains from one side often offset losses from the other.
- **Leveraging Price Inefficiencies**: The strategy takes advantage of temporary inefficiencies in the prices of the paired securities, which are expected to eventually revert to their mean.

## Execution

- **Setup Conditions**: Before implementing the strategy, ensure that both securities are closely monitored for significant divergences that could trigger trades.
- **Operational Dynamics**: Continuous monitoring and recalibration of the threshold levels for buying and selling based on historical data and market conditions are crucial for the strategy's success.

The presented schema not only outlines a robust framework for pair trading but also highlights the importance of mathematical tools like rounding in simplifying complex trading decisions.
