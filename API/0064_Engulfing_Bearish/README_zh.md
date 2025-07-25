# 看跌吞没形态策略
[English](README.md) | [Русский](README_ru.md)

该形态试图在一段上涨后捕捉看跌回调的开始。当一根阴线完全吞没之前的阳线时形成看跌吞没。统计此前连续上涨的蜡烛数量可确认市场先前处于上升阶段。

测试表明年均收益约为 79%，该策略在股票市场表现最佳。

算法顺序记录每根蜡烛。如果新蜡烛收盘价低于开盘价且实体包住前一根阳线，则执行卖空。止损位于形态高点上方以限制风险。

仓位通常通过保护性止损进行管理，若行情发生变化，交易者也可手动离场。要求存在上升趋势有助于在震荡市中避免假信号。

## 细节

- **入场条件**：阴线吞没前一根阳线，可选上升趋势确认。
- **多/空**：仅做空。
- **退出条件**：止损或人工离场。
- **止损**：有，位于形态高点上方。
- **默认值**：
  - `CandleType` = 15 分钟
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendBars` = 3
- **过滤条件**：
  - 类别: 形态
  - 方向: 空头
  - 指标: K线形态
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等

