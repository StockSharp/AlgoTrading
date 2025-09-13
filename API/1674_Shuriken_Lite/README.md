# Shuriken Lite Strategy

This strategy replicates the functionality of the original *Shuriken Lite* MQL tool. It tracks executed trades on the account and groups them by numerical identifiers known as **magic numbers**. For each group the strategy calculates:

- Number of trades
- Winning and losing trades
- Total profit or loss in pips
- Profit factor

The statistics are logged after every new trade when score display is enabled.

## Parameters

- **Magic Numbers** — comma-separated list of identifiers used to group trades. Each identifier should match the numeric value placed in the order comment.
- **Show Scores** — enable or disable logging of statistics.

## Usage

1. Set the desired magic numbers in the parameter.
2. Run the strategy alongside other strategies that set numeric comments on their orders.
3. Check the log for the aggregated performance metrics.
