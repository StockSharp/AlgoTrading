# Exp GStop Strategy

## Overview
Exp GStop is a risk management strategy that monitors portfolio equity and closes open positions when a specified profit or loss threshold is reached.

## Parameters
- **Mode**: Determines whether thresholds are expressed as percentages or currency.
- **Stop Loss**: Maximum acceptable loss value.
- **Take Profit**: Profit target value.
- **Candle Type**: Candle type used for periodic checks.

## Logic
On start the strategy stores the initial portfolio value. After each finished candle it calculates current profit. When profit reaches or exceeds the configured bounds the strategy closes the entire position. Once no position remains the stop flag is reset allowing new trades.
