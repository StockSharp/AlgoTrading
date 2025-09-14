# Range Expansion Index Strategy

This strategy uses Tom DeMark's **Range Expansion Index (REI)** to evaluate price strength and weakness.
The indicator compares current highs and lows with previous prices and oscillates between positive and negative values.

## How It Works

- When the REI rises above the **Down Level** (default `-60`) after being below it, the strategy opens a **long** position.
- When the REI falls below the **Up Level** (default `60`) after being above it, the strategy opens a **short** position.
- Opposite positions are closed automatically when an opposite signal occurs.

## Parameters

- `REI Period` – number of bars used in the REI calculation (default `8`).
- `Up Level` – upper threshold that indicates price weakness when crossed downward (default `60`).
- `Down Level` – lower threshold that indicates price strength when crossed upward (default `-60`).
- `Candle Type` – timeframe of candles for indicator calculation (default `8 hours`).

## Usage

Attach the strategy to a security and start it. The strategy subscribes to the specified candle series and uses market
orders to enter or exit positions based on REI signals.
