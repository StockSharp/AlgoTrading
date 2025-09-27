# Random Bias Trader 策略
[English](README.md) | [Русский](README_ru.md)

Random Bias Trader 策略通过 StockSharp 高层 API 复刻 MetaTrader 的“random trader”专家顾问。
每根已完成的K线都会抛掷一次虚拟硬币，在没有持仓时按照结果开多或开空。
止损和止盈可以来自 ATR(10) 的倍数，也可以使用固定点数，并按收益/风险比进行放大。
仓位规模基于账户风险百分比计算，同时受交易品种最小/最大手数限制。
启用保本后，当浮盈达到指定点数时，止损会自动移动到开仓价。

## 详情
- **数据**：由 `CandleType` 指定的一组蜡烛数据。
- **入场条件**：
  - 多头：当前无持仓，抛硬币得到多头，按最近收盘价买入。
  - 空头：当前无持仓，抛硬币得到空头，按最近收盘价卖出。
- **离场条件**：
  - 止损：`LossPipDistance` × 点值或 `LossAtrMultiplier` × ATR(10)，取决于 `LossType`。
  - 止盈：在止损距离基础上乘以 `RewardRiskRatio`。
  - 保本：启用时，盈利达到 `BreakevenDistancePips` 点后将止损移至开仓价。
- **止损**：每笔交易都会设置动态止损与止盈，可选保本功能。
- **默认参数**：
  - `CandleType` = 1 分钟周期
  - `RewardRiskRatio` = 2.0
  - `LossType` = Pip
  - `LossAtrMultiplier` = 5.0
  - `LossPipDistance` = 20 点
  - `RiskPercentPerTrade` = 1%
  - `UseBreakeven` = 启用
  - `BreakevenDistancePips` = 10 点
  - `UseMaxMargin` = 启用
- **筛选标签**：
  - 类型：随机 / 趋势中性
  - 方向：双向，由抛硬币决定
  - 指标：ATR(10)（可选）
  - 复杂度：入门
  - 风险等级：中等，随止损宽度变化

## 说明
- 如果基于风险的仓位过小，可选择使用合约允许的最大手数进行下单。
- 下单前会将止损与止盈价格按最小价格步长取整。
- 保本机制保证任意时刻只有一笔仓位，与原版 MetaTrader 策略保持一致。
