# Charles SMA Trailing 策略
[English](README.md) | [Русский](README_ru.md)

该策略利用两条简单移动平均线的交叉并可选使用跟踪止损。当快速 SMA 上穿慢速 SMA 时开多单；当快速 SMA 下穿慢速 SMA 时开空单。策略支持固定的止损、止盈以及在达到预设盈利后激活的跟踪止损。

## 细节

- **入场条件**：
  - 快速 SMA 上穿慢速 SMA → 做多。
  - 快速 SMA 下穿慢速 SMA → 做空。
- **多空方向**：双向。
- **离场条件**：
  - 反向交叉。
  - 触发止损或止盈。
  - 当盈利达到 `TrailStart` 后启动跟踪止损，间距为 `TrailingAmount`。
- **止损/止盈**：
  - `StopLoss` 定义固定的止损价格距离。
  - `TakeProfit` 定义固定的止盈目标。
  - `TrailStart` 与 `TrailingAmount` 控制跟踪止损。
- **默认参数**：
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `StopLoss` = 0
  - `TakeProfit` = 25
  - `TrailStart` = 25
  - `TrailingAmount` = 5
- **过滤器**：
  - 类型：趋势跟随
  - 方向：多 & 空
  - 指标：SMA
  - 止损：是
  - 复杂度：中等
  - 时间周期：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
