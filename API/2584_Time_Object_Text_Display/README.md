# Time Object Text Display Strategy

## Overview

This strategy is a direct conversion of the MetaTrader 4 script **time_objtxt.mq4**. The original script iterated over all text objects (`OBJ_TEXT`) available on the active chart and printed the timestamp associated with each object. There was no trading logic involved; the tool simply reported annotation times to the log.

The StockSharp version keeps the same spirit. At start-up it reads a configurable list of text object definitions, parses the timestamps, and writes informative messages to the strategy log. This allows you to document key events or notes on the chart and review their timestamps inside the StockSharp environment.

## Key characteristics

- **No trading operations.** The strategy is informational only and never sends market or limit orders.
- **Single configuration parameter.** `TextObjectDefinitions` is a semicolon-separated list where each entry has the format `Name@Time` (for example, `My Text@2015.06.30 11:53:24`).
- **Flexible date parsing.** Multiple common MetaTrader date formats are supported, including variants with dashes, dots, and optional time zone offsets.
- **Detailed logging.** For every valid entry the strategy prints `The time of object <Name> is <timestamp>` just like the original script.
- **Input validation.** Malformed entries trigger warning messages so that typos can be corrected quickly.

## Parameter reference

| Parameter | Description | Notes |
|-----------|-------------|-------|
| `TextObjectDefinitions` | Semicolon-separated list of text annotations to inspect. Each element must contain a name and a timestamp joined by `@`. | Default value: `My Text@2015.06.30 11:53:24`. Leading/trailing spaces are ignored. |

### Supported timestamp formats

The parser accepts the following patterns (white space is optional):

- `yyyy-MM-dd HH:mm:ss`
- `yyyy-MM-ddTHH:mm:ss`
- `yyyy-MM-dd HH:mm:ss zzz`
- `yyyy-MM-ddTHH:mm:sszzz`
- `yyyy.MM.dd HH:mm:ss`
- `yyyy.MM.ddTHH:mm:ss`
- `yyyy.MM.dd HH:mm:ss zzz`
- `yyyy.MM.ddTHH:mm:sszzz`

If none of these formats match, a fallback attempt with the current culture is performed. When parsing fails, the strategy logs `Failed to parse time '<value>' for text object '<name>'.`

## Workflow

1. Configure `TextObjectDefinitions` with the annotations you want to review. Separate multiple entries with `;`.
2. Start the strategy. During `OnStarted` it outputs an informational message and processes each definition.
3. Review the log to see the timestamps for every valid text object.

## Differences from the MQL script

- StockSharp does not expose MetaTrader chart objects directly, so the strategy reads user-provided definitions instead of scanning the chart automatically.
- Logging uses StockSharp's `LogInfo` and `LogWarning` helpers.
- Date parsing supports several regional formats so that the behaviour matches `TimeToString(..., TIME_DATE | TIME_SECONDS)` from MetaTrader.

## Practical tips

- Use descriptive names (`SessionStart`, `NewsRelease`, etc.) to make the log entries meaningful.
- Keep the list compact to avoid cluttering the log with outdated annotations.
- Because no orders are generated, this strategy can safely run alongside other trading algorithms as an informational companion.
