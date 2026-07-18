# Pineconnector Strategy Template
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy demonstrates how to connect any indicator to generate trading signals. It uses two moving averages as an example and enters long when the fast average crosses above the slow average, and enters short on the opposite cross.

## Parameters
- **Fast Length** – period of the fast moving average.
- **Slow Length** – period of the slow moving average.
- **Candle Type** – candle type for calculation.
