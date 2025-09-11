# MH Hull Moving Average Based Trading
[English](README.md) | [Русский](README_ru.md)

基于Hull移动平均的突破策略。

策略比较开盘价与Hull移动平均生成的动态水平。当价格突破上方水平时做多，跌破下方水平时做空。相反方向的突破将平掉当前仓位。

## 详情

- **入场条件**：价格相对于Hull MA水平。
- **多/空**：双向。
- **出场条件**：相反突破。
- **止损**：无。
- **默认值**：
  - `HullPeriod` = 210
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选**：
  - 类别：趋势
  - 方向：双向
  - 指标：MA
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内 (5m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
