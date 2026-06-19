# John Bob Trading Bot 策略
[English](README.md) | [Русский](README_ru.md)

结合50根K线高低点与公平价值缺口检测的突破策略。开仓五次，使用ATR止损并设定多个获利目标。

## 详情

- **入场条件**：
  - 做多：价格上穿50柱最低价或出现看涨FVG
  - 做空：价格下穿50柱最高价或出现看跌FVG
- **多空**：双向
- **出场条件**：
  - 价格到达五个TP之一
  - 价格触及ATR止损
- **止损**：ATR倍数
- **默认值**：
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 分类：Breakout
  - 方向：双向
  - 指标：ATR、Highest、Lowest
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
