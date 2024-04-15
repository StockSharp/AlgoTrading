# Pyramiding Strategy Schema

This schema explores a pyramiding strategy that builds upon the principles outlined in the previous lesson's trading strategy. It focuses on increasing a position's size in a winning trade to maximize potential gains.

## Overview

Pyramiding is a method where traders increase their position size by using unrealized profits from successful trades. This strategy enhances potential returns without committing additional capital upfront.

## Strategy Logic

- **Initial Position**: The strategy begins with a base position size determined by the initial trading conditions outlined in the previous strategy.
- **Incremental Increases**: As the trade moves in the desired direction and accumulates unrealized profits, the strategy incrementally increases the position size at predetermined profit thresholds.
- **Risk Management**: Each new position added is smaller than the previous one, ensuring that the risk does not compound excessively.

## Key Features

- **Profit-Based Scaling**: The strategy scales up the position size based on the profits being realized, which aligns the potential reward with the performance of the asset.
- **Controlled Exposure**: By capping the size of each additional position, the strategy maintains a controlled exposure to the market, helping manage overall risk.

## Application and Benefits

- **Capital Efficiency**: Pyramiding allows traders to leverage their successful trades to increase returns without needing additional capital.
- **Dynamic Position Sizing**: Adjusts position sizes dynamically based on market performance, allowing traders to capitalize on favorable market conditions.

## Execution

- **Criteria for Adding Positions**: The strategy defines clear criteria based on the performance of the initial trade before additional positions are added.
- **Profit Lock-in Mechanism**: Includes mechanisms to lock in profits, such as trailing stops or profit targets, to protect gains as the position size increases.

The pyramiding strategy schema elaborated here uses the foundational strategy from the previous lesson as a springboard to introduce more advanced trade management techniques. This approach not only aims to maximize returns but also to integrate stringent risk controls, ensuring that the potential downside is kept in check as positions are scaled up.
