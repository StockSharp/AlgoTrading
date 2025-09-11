# Pullback Pro Dow 策略
[English](README.md) | [Русский](README_ru.md)

该策略利用道氏理论的枢轴点确定趋势方向，并在 EMA 回调时结合 ADX 趋势强度过滤入场。系统在两个风险回报目标处分批止盈。

回测显示，在如 US30 等指数期货上表现稳定。

## 详情

- **入场条件**：
  - 多头：形成更高高点和更高低点，最低价下穿 EMA，且 ADX 高于阈值
  - 空头：形成更低高点和更低低点，最高价上穿 EMA，且 ADX 高于阈值
- **多空**：双向
- **出场条件**：止损设在最后一个枢轴点，两档风险回报目标止盈
- **止损**：基于枢轴点
- **默认值**：
  - `PivotLookback` = 10
  - `EmaLength` = 21
  - `RiskReward1` = 1.5m
  - `Tp1Percent` = 50
  - `RiskReward2` = 3m
  - `UseAdxFilter` = true
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：EMA，Average Directional Index
  - 止损：有
  - 复杂度：中等
  - 周期：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
