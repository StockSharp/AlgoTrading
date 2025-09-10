# 3x Supertrend
[English](README.md) | [Русский](README_ru.md)

**3x Supertrend** 使用三个不同周期和倍数的 ATR 线。
当价格站上所有三条线且快速线转为上行时开多仓；
当价格跌破所有线时平仓，表示多头动能结束。

## 细节
- **数据**: 价格K线。
- **入场条件**: 价格在所有线上方且快速线转多。
- **离场条件**: 价格跌破所有线。
- **止损**: 无。
- **默认参数**:
  - `AtrPeriod1` = 11
  - `Factor1` = 1
  - `AtrPeriod2` = 12
  - `Factor2` = 2
  - `AtrPeriod3` = 13
  - `Factor3` = 3
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 仅多头
  - 指标: 基于 ATR 的 Supertrend
  - 复杂度: 中等
  - 风险级别: 中等
