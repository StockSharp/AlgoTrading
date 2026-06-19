# Supertrend - SSL 策略（带切换）
[English](README.md) | [Русский](README_ru.md)

该策略结合 Supertrend 指标与 SSL 通道。
可选开关允许在入场前要求两个指标同时确认。
如果开启确认，任一指标的首个信号会等待另一指标确认后才执行。
当任一指标出现反向信号时平仓。

## 详情

- **指标**: Supertrend (ATR 10, 系数 2.4)、SSL 通道 (周期 13)
- **入场**: SSL 交叉或 Supertrend 方向变化，可选双重确认
- **出场**: SSL 或 Supertrend 的反向信号
- **方向**: 多空皆可
- **周期**: 任意
