# Screenshot Strategy

## Overview
The strategy reproduces the MetaTrader 5 example that waits for the **S** key and saves a chart screenshot. In StockSharp the strategy listens for keyboard events from the hosting console application. When the **S** key is pressed it generates a PNG image that contains a placeholder rendering with the capture timestamp and the currently bound security identifier. Autoscroll and shift flags that exist in MetaTrader do not have direct equivalents, therefore the StockSharp version focuses on the screenshot workflow.

The generated file is placed inside the configured output folder (relative to the application base directory by default) with the pattern `<ScreenshotName>_YYYYMMDD_HHMMSS.png`. Each capture updates the informational log so that the desktop UI can surface successful operations or errors.

## Parameters
- `ScreenshotName` – base name used to build the file name. Invalid characters are replaced with underscores.
- `ScreenshotWidth` – horizontal size of the generated PNG image in pixels. Values less than one are automatically promoted to one.
- `ScreenshotHeight` – vertical size of the generated PNG image in pixels. Values less than one are automatically promoted to one.
- `OutputFolder` – folder that stores the screenshots. When a relative path is provided the strategy combines it with the process base directory.

All parameters are exposed through `StrategyParam<T>` so they can be edited at runtime. Optimization is disabled because the logic is purely utility-focused.

## Usage
1. Attach the strategy to a connector and select the instrument you want to display on the chart.
2. Start the strategy. A background task begins polling the console keyboard buffer.
3. Press the **S** key inside the console window that hosts the strategy. Each press triggers the screenshot routine.
4. Inspect the generated PNG files inside the configured folder. The file contains diagnostic text (timestamp and security identifier) because StockSharp does not provide direct chart rendering in headless mode.

The keyboard listener is cancelled when the strategy stops. If the process does not expose a console input stream (for example, the strategy is executed inside a GUI host) a warning is issued once and the keyboard shortcut is ignored to mirror the MQL behaviour where the event simply does not fire.

## Notes
- The implementation uses `System.Drawing` to draw the placeholder image. On non-Windows platforms this namespace requires the appropriate native dependencies (libgdiplus).
- When the output directory cannot be created or the file cannot be saved the exception is logged through `AddErrorLog`, allowing the user interface to inform the operator.
- The logic never sends trading orders, keeping parity with the reference MetaTrader expert advisor whose `OnTick` handler is empty.
