# Delta MFI 策略
[English](README.md) | [Русский](README_ru.md)

该策略比较快速和慢速 Money Flow Index 指标的值。当快速 MFI 高于慢速 MFI 且慢速 MFI 高于信号水平时做多；当快速 MFI 低于慢速 MFI 且慢速 MFI 低于 `100 - Level` 时做空。

## 细节

- **入场条件**：
  - 当 `slow MFI > Level` 且 `fast MFI > slow MFI` 时买入
  - 当 `slow MFI < 100 - Level` 且 `fast MFI < slow MFI` 时卖出
- **多空方向**：双向
- **离场条件**：反向信号
- **止损**：无
- **默认值**：
  - `FastPeriod` = 14
  - `SlowPeriod` = 50
  - `Level` = 50
  - `CandleType` = 4 小时K线
- **过滤条件**：
  - 分类：指标
  - 方向：双向
  - 指标：Money Flow Index
  - 止损：无
  - 复杂度：基础
  - 时间框架：H4
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
