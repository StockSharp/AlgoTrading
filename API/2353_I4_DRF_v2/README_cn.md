# I4 DRF v2 策略
[English](README.md) | [Русский](README_ru.md)

I4 DRF v2 策略使用 i4_DRF_v2 指标，该指标统计最近周期内上涨和下跌的收盘数。
根据 TrendMode 参数，可选择反趋势模式（Direct）或顺趋势模式（NotDirect）。
当指标符号翻转时开仓或平仓，并支持以价格步长计的止损和止盈。

## 细节

- **入场条件**：根据 TrendMode 的指标符号翻转
- **多空方向**：双向
- **出场条件**：反向信号或止损/止盈
- **止损**：有
- **默认值**:
  - `Period` = 11
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `TrendMode` = Direct
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: 自定义
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
