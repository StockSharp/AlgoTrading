# Working Time and Time-Based Strategy Schema

This schema file demonstrates the application of the "Working Time" cube along with other relevant cubes in the Designer platform to implement time-based trading strategies.

## Overview

The schema explores various configurations using the "Working Time" cube, which allows traders to execute strategies based on specific time conditions.

## Key Components

- **Working Time Cube**: Used to define the active trading hours or specific times for executing trades.
- **Variable Cube**: Named "Strategy", this cube is used to store and manipulate strategy-specific variables.
- **Converter Cube**: Utilized for converting and retrieving time-related data to support time-based decisions.

## Strategy Details

### Strategy with Working Time Condition
- **Pre-Closing Purchase**: The strategy initiates a buy order one minute before the closing of the defined working hours, aiming to capitalize on potential price movements at the end of the trading session.

### Specific Time Trigger
- **Fixed Time Purchase**: Implements a purchase at exactly 18:00, aligning the trade execution with significant market events or typical closing times.

### Advanced Time-Based Closure from Lesson 7
- **Position Closing**: Closes any open positions five minutes before the end of the working hours, a strategy designed to avoid holding overnight positions or reacting to end-of-day price fluctuations.

## Note on Version 5 Changes

In the fifth version of the Designer software, there are enhancements to how time calculations and the "Working Time" cube function together. After importing strategies that utilize these features, it is recommended to recreate them within the platform to ensure correct functionality and to take advantage of the updated time calculation formulas.

This schema provides a comprehensive framework for developing and testing strategies that rely heavily on precise timing for trade execution, making it an essential tool for traders focusing on intraday trading strategies or those needing to adhere to specific market hours.
