# Volume-Weighted Supertrend Strategy
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略基于成交量加权均线与 ATR 生成超级趋势，并对成交量应用第二个超级趋势以确认方向。当价格和成交量趋势同时向上时开多，在条件反转时平仓。

## 参数
- **ATR Period** – 价格趋势的 ATR 周期。
- **Volume Period** – VWAP 与成交量趋势的周期。
- **Factor** – ATR 倍数。
- **Candle Type** – 使用的蜡烛类型。
