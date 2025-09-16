# Vortex 指标交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略利用 Vortex 指标的 VI+ 与 VI- 交叉进行交易。
当 VI+ 上穿 VI- 时做多；当 VI- 上穿 VI+ 时做空。
止损和止盈以价格步长自动管理。

## 参数

- **Vortex Length** – Vortex 指标周期。
- **Candle Type** – 计算指标的蜡烛时间框架。
- **Stop Loss** – 以价格步长表示的止损。
- **Take Profit** – 以价格步长表示的止盈。

## 细节

- **指标**: Vortex
- **方向**: 多空
- **时间框架**: 可配置
- **风险管理**: 通过 `StartProtection` 设置止损和止盈。
