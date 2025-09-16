# News Trading EA Strategy
[English](README.md) | [Русский](README_ru.md)

基于新闻事件的时间型对冲策略。在预定时间于当前价格上下固定距离同时挂出买入止损和卖出止损订单。在激活窗口内，每根K线更新订单以跟随价格。若有持仓，则取消相反的挂单，并根据止盈、止损或订单到期退出。

## 详情

- **入场条件**：
  - 在对冲窗口内于 close + Distance * step 挂买入止损，在 close - Distance * step 挂卖出止损。
- **做多/做空**：双向
- **出场条件**：相反挂单、止盈/止损或订单到期
- **止损**：固定止损与止盈
- **默认值**：
  - `StartDateTime` = DateTime.Now
  - `StartStraddle` = 0
  - `StopStraddle` = 15
  - `Volume` = 0.01m
  - `Distance` = 55m
  - `TakeProfit` = 30m
  - `StopLoss` = 30m
  - `Expiration` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤**：
  - 类别: News
  - 方向: 双向
  - 指标: 无
  - 止损: 有
  - 复杂度: 初级
  - 时间框架: 事件
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 高
