# MasterMind 2 策略

## 概述
MasterMind 2 策略来源于 MQL4 平台上的 "TheMasterMind2" 智能交易程序。策略等待 Stochastic Oscillator 与 Williams %R 同时给出极端信号来判断市场耗竭。当两个指标同时显示极度超卖时建立多头仓位，当两者同时显示极度超买时建立空头仓位。所有判断均在 K 线收盘后进行，与原始程序完全一致。

## 指标
- **Stochastic Oscillator**：使用较长的观察窗口来衡量超买和超卖区域，比较的是 %D 信号线。
- **Williams %R**：作为确认过滤条件，要求多头信号时接近 -100，空头信号时接近 0。

## 入场规则
1. 等待当前 K 线收盘。
2. 计算 Stochastic，并获取 %D 信号值。
3. 计算 Williams %R。
4. **做多**：若 `%D < 3` 且 `Williams %R < -99.9`，先平掉所有空单，然后买入。
5. **做空**：若 `%D > 97` 且 `Williams %R > -0.1`，先平掉所有多单，然后卖出。

## 离场规则
- 进场后根据设定的点数距离放置止损与止盈。
- 启用追踪止损时，价格按照设定的步长继续向盈利方向移动时会收紧保护价位。
- 启用保本功能后，价格达到指定利润距离时会把止损移动到开仓价。
- 出现反向信号时会先平掉现有仓位，再考虑新的方向。

## 参数
- `Trade Volume`：每次市价单的交易量。
- `Stochastic Period`、`Stochastic %K`、`Stochastic %D`：Stochastic 指标的相关参数。
- `Williams %R Period`：Williams %R 的计算周期。
- `Stop Loss`、`Take Profit`：以点数表示的止损和止盈距离。
- `Trailing Stop`、`Trailing Step`：追踪止损的距离和调节步长。
- `Break Even`：将止损移动到开仓价所需的利润距离。
- `Candle Type`：计算时使用的 K 线类型或时间框架。

## 说明
- 策略只在蜡烛线状态为 `Finished` 时执行信号，避免未完成的 K 线触发交易。
- 所有订单均以 `Trade Volume` 指定的合约数量按市价成交。
- 将距离参数设置为 0 可以关闭对应的保护功能。
