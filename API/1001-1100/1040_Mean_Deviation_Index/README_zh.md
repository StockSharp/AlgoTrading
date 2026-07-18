# Mean Deviation Index 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略利用平均偏差指数(MDX) 交易与 ATR 过滤 EMA 的偏离。
当 MDX 高于设定水平时做多，当 MDX 低于负水平时做空。

## 细节

- **入场**：
  - MDX > Level 时做多
  - MDX < -Level 时做空
- **出场**：反向信号
- **指标**：EMA 与 ATR
