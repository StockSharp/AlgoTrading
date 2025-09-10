# AO/AC 交易区策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 “AO/AC Trading Zones” 概念。它结合 Awesome Oscillator (AO)、Acceleration/Deceleration (AC) 以及威廉姆斯分形，在动能加速并突破齿线后逐步加仓多头。

## 细节

- **入场**：至少连续两根柱满足 `close > teeth`、`AO > AO[1]`、`AC > AC[1]` 且 `close > EMA`。
- **加仓**：条件成立时最多增加五次多头头寸。
- **出场**：分形趋势反转或价格跌破止损。
- **指标**：SMMA（齿线）、AO、AC、EMA。
- **止损**：第五个绿色柱的最低价。
