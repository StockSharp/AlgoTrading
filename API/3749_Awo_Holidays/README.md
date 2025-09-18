# Awo Holidays Strategy

## Overview

The **AwoHolidaysStrategy** reproduces the behaviour of the MetaTrader "awo Holidays" indicator inside StockSharp. It reads a holiday calendar from a CSV file, detects weekends, and publishes a multi-day schedule through the strategy log so that you always know when trading sessions will be affected by swaps, weekends, or public holidays. The log output mirrors the original MQL comment block by listing tomorrow, today, and a configurable number of previous days together with optional descriptive colour labels.

Unlike trading strategies, this component does not submit orders. It acts as a chart and status overlay that keeps the operator informed about upcoming market interruptions. The indicator can be started on any security and time-frame, but daily candles are recommended because they align with the original tool.

## Holiday file format

The strategy expects a semicolon-separated CSV file with four columns:

| Column | Description |
| ------ | ----------- |
| `Date` | Calendar date in `yyyy.MM.dd` format (alternatively `yyyy-MM-dd` or `dd.MM.yyyy`). |
| `Country` | Country or market where the holiday is observed. |
| `Symbols` | Comma-separated list of symbols affected by the holiday. Leave empty to apply to all instruments. |
| `Holiday` | Human-readable holiday name. |

Example line:

```
2024.12.25;United States;EURUSD,USDJPY;Christmas Day
```

If the active security identifier contains any of the tokens listed in the `Symbols` column, the holiday is reported as `Holiday in Country`. When the file cannot be located a warning is written and only weekend detection remains active.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `History Depth` | Number of previous calendar days to include after the future-oriented lines. | `3` |
| `Clear On Stop` | When enabled, the cached status text is cleared once the strategy stops. | `true` |
| `Holiday File` | Relative or absolute path to the CSV holiday calendar. | `holidays.csv` |
| `Candle Type` | Candle series that triggers updates. Daily candles are recommended. | `1 day` time-frame |
| `Workday Label` | Text label used to highlight workdays. | `LightBlue` |
| `Weekend Label` | Text label used to highlight Saturdays and Sundays. | `Blue` |
| `Holiday Label` | Text label used when a matching holiday is detected. | `DarkOrange` |

## Usage

1. Prepare a CSV file that follows the format above and place it inside the terminal folder or provide an absolute path through the `Holiday File` parameter.
2. Attach the strategy to a security and verify that the `Candle Type` corresponds to the time-frame you wish to monitor.
3. Start the strategy. After the first finished candle, the log will contain lines similar to:
   ```
   Holiday overview:
   2024-06-15 | Tomorrow | saturday | - | Blue
   2024-06-14 | Today | workday | Flag Day in United States | DarkOrange
   2024-06-13 | Yesterday | workday | - | LightBlue
   2024-06-12 | 2 days ago | workday | - | LightBlue
   2024-06-11 | 3 days ago | workday | - | LightBlue
   ```
4. Monitor the strategy log to stay aware of weekends and holidays. Update the CSV file whenever new holidays become available and restart the strategy to reload them.

## Notes

- The symbol comparison is case-insensitive and accepts partial matches to stay compatible with the original MQL implementation.
- Set `History Depth` to zero if you only want to see tomorrow and today.
- The colour labels are descriptive strings. You can replace them with any text that matches your charting conventions.
- When the CSV file is missing or contains unparsable dates, the affected rows are skipped. A summary of successfully loaded entries is logged for troubleshooting.
