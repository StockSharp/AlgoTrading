# HPCS Inter1 Initialization Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **HPCS Inter1 Initialization Strategy** replicates the behavior of the original MetaTrader expert by reading the first line of a CSV file when the strategy starts and logging every token that is separated by an underscore character. This setup is useful for validating connectivity to shared folders, confirming that automation scripts generate the expected configuration line, or preparing more advanced workflows that depend on external preprocessing steps.

Unlike most trading algorithms, this strategy does not submit orders. It focuses on the infrastructure side of a trading environment by making sure the workstation can access text files created by other processes (for example, parsers, scanners, or human operators). Each token is written to the strategy log so that the operator can inspect the data directly inside the StockSharp interface.

## How it works

1. When the strategy starts, it resolves the path of the configured CSV file. Missing or empty file names are reported through warnings.
2. If the file does not exist, the strategy stays idle and emits a warning to highlight the missing dependency.
3. The first line is read with UTF-8 encoding. Empty or whitespace-only lines are reported to avoid silent misconfigurations.
4. The line is split using the configurable separator (default: underscore). Every token is written to the log in the same order as it appears in the file.
5. Any exception triggered during file access is captured and forwarded to the log for rapid troubleshooting.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `CsvFileName` | Name or path of the CSV file to read when the strategy starts. | `Third.csv` |
| `Separator` | Character that splits the first line into tokens before logging. | `_` |

## Recommended usage

- Place the strategy in a workspace that requires checking whether a companion process exported the latest configuration snapshot.
- Combine it with scheduled tasks that rewrite `Third.csv` to notify operators about the contents of the last export.
- Replace the default separator when the file uses a different delimiter (for example, commas or semicolons).
- Extend the strategy if you need to trigger additional actions after the tokens are parsed, such as scheduling trades or updating in-memory parameters.

## Logging example

If the first line of `Third.csv` contains `SYMBOL_EURUSD_1.2345_LONG`, the strategy produces the following log entries:

```
Original line: SYMBOL_EURUSD_1.2345_LONG
Token 1: SYMBOL
Token 2: EURUSD
Token 3: 1.2345
Token 4: LONG
```

These log messages confirm that the environment is ready for more advanced automation steps that rely on external CSV data.
