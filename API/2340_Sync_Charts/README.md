# Sync Charts Strategy

This strategy demonstrates how to keep multiple charts visually synchronized. One chart acts as the master and shares its parameters with all other charts.

## Features

- Copies scale, chart mode, time frame and first visible bar from the master chart.
- Optionally replicates all vertical lines drawn on the master chart.
- Uses a timer with 200 ms interval to update the targets.

## Parameters

| Name | Description |
| ---- | ----------- |
| `SyncVerticalLines` | When enabled, vertical lines from the master chart are cloned to the rest. |

## Usage

1. Create an instance of the strategy and add all charts via `AddChart`. The first chart added becomes the master chart.
2. Start the strategy to begin periodic synchronization.
3. Draw vertical lines or change the view on the master chart to see updates on other charts.

This example focuses on chart management and contains no trading logic. It can be expanded with trading rules if required.
