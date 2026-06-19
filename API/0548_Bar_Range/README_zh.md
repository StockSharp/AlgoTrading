# Bar Range策略
[English](README.md) | [Русский](README_ru.md)

Bar Range策略在当前K线范围位于近期最高百分位且收盘价低于开盘价时做多，并在固定数量的K线后平仓。

## 细节

- **入场条件**：
  - 范围 = 最高价 − 最低价
  - 范围在`LookbackPeriod`内的百分位 ≥ `PercentRankThreshold`
  - 收盘价 < 开盘价
- **出场条件**：在`ExitBars`根K线后平仓。
- **默认参数**：
  - `LookbackPeriod` = 50
  - `PercentRankThreshold` = 95
  - `ExitBars` = 1
- **过滤器**：
  - 类型：波动率
  - 方向：多头
  - 指标：Percent Rank
  - 止损：否
  - 复杂度：低
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

