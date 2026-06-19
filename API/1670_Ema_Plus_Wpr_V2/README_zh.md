# EMA 加 WPR v2 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 Williams %R 振荡器与 EMA 趋势过滤相结合。在回调后 WPR 触及极值时入场。支持基于 WPR 的退出、追踪止损以及按条数退出。

## 细节

- **做多**：WPR 在回调后触及 -100 且 EMA 趋势向上。
- **做空**：WPR 在回调后触及 0 且 EMA 趋势向下。
- **指标**：Williams %R、EMA。
- **止损**：固定止损和止盈，可选追踪止损。
