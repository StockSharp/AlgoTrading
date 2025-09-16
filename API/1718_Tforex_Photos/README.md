# T-foREX Photos Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy monitors the number of active orders for the selected security. Whenever the count changes, it logs an informational message. The original MQL version captured chart screenshots; this C# adaptation only logs the event but can be extended to add screenshot functionality.

## Details

- **Entry Criteria**: None, the strategy does not open positions automatically.
- **Long/Short**: Not applicable.
- **Exit Criteria**: None.
- **Stops**: No.
- **Filters**: Triggers only when the number of active orders changes.
