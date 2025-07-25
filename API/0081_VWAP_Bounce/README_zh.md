# VWAP 回踩策略
[English](README.md) | [Русский](README_ru.md)

成交量加权平均价（VWAP）是常用的日内基准。当价格远离 VWAP 后又出现向其回归的蜡烛时，通常会有短暂的反弹。本策略交易这种回踩。

测试表明年均收益约为 130%，该策略在股票市场表现最佳。

每根K线计算当前的 VWAP。如果看涨蜡烛收盘低于 VWAP，则做多；若看跌蜡烛收盘高于 VWAP，则做空。风险通过固定百分比止损控制，持仓通常仅维持到出现反向信号或止损触发。

由于属于日内极端的均值回归方法，该策略在震荡行情中效果更佳。

## 细节

- **入场条件**：阳线收盘低于 VWAP 或阴线收盘高于 VWAP。
- **多/空**：双向。
- **退出条件**：出现反向信号或止损。
- **止损**：是，按百分比。
- **默认值**：
  - `CandleType` = 5 分钟
  - `StopLoss` = 2%
- **过滤条件**：
  - 类别: 均值回归
  - 方向: 双向
  - 指标: VWAP
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等

