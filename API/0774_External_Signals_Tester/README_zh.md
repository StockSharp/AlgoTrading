# 外部信号测试策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

当快慢 EMA 的差值穿越零或价格穿越可选 EMA 线时开仓。支持百分比止损、止盈和保本。

## 细节

- **入场条件**：EMA(10) - EMA(30) 穿越零或价格穿越 EMA。
- **多空方向**：多头和空头。
