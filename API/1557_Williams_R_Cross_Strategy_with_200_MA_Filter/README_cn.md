# Williams %R Cross 策略配合 200 MA 过滤
[English](README.md) | [Русский](README_ru.md)

该策略利用 Williams %R 在 -50 附近的交叉，并结合 200 期 SMA 作为趋势过滤。
仓位通过固定的止盈和止损退出。

## 细节

- **入场条件**: %R 在价格相对 200 SMA 的情况下交叉阈值
- **多空方向**: 双向
- **出场条件**: 止盈或止损
- **止损**: 是
- **默认值**:
  - `WrLength` = 14
  - `CrossThreshold` = 10
  - `TakeProfit` = 30
  - `StopLoss` = 20
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: WilliamsR, SMA
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

