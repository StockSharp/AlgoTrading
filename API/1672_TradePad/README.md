# TradePad Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

TradePad Strategy is a manual trading panel ported from the original MQL TradePad expert. The strategy sets up a panel to manage trades interactively. It processes tick data, trade notifications, timer events and chart messages without automated entry or exit rules.

This sample demonstrates how to build a discretionary trading interface on top of StockSharp.

## Details

- **Entry Criteria**: Manual order placement through the panel.
- **Long/Short**: Both, depending on user action.
- **Exit Criteria**: Manual position closing.
- **Stops**: None; user may implement custom logic.
- **Filters**: No automatic filters.
