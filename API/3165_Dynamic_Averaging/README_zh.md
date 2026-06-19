# 动态均值策略

## 概述
“Dynamic Averaging” 源自 MetaTrader 5 指标专家顾问“Dynamic averaging.mq5”（id 23319）。策略将快速随机指标与基于标准差的波动率过滤器结合使用。只有当当前波动率低于其滑动平均值时才允许交易，从而把入场限制在盘整阶段，让随机指标的反转信号更加可靠。

## 参数
- **TradeVolume** – 每次开仓的基础手数。发生亏损后自动加倍，盈利或持平后恢复原始数值。
- **MinimumProfit** – 当浮动盈利（账户货币）超过该数值时立即平掉全部头寸。
- **SlidingWindowDays** – 用于计算标准差均值的日历天数，决定波动率基准窗口的长度。
- **StochasticKPeriod** – 计算 %K 的回溯周期。
- **StochasticDPeriod** – %D 线的平滑周期。
- **StochasticSlowPeriod** – 随机指标的最终平滑参数。
- **StdDevPeriod** – 标准差指标的窗口长度。
- **CandleType** – 指标所用的K线类型（默认 15 分钟）。

## 交易逻辑
1. 策略仅在完整 K 线上运行，通过 `SubscribeCandles().BindEx` 同步更新随机指标与波动率过滤器。
2. 计算 `StandardDeviation(StdDevPeriod)`，并与 `SimpleMovingAverage` 在最近 `SlidingWindowDays` 天内的均值进行比较。
3. 若当前标准差大于该均值，则跳过本根K线。
4. 当波动率受限时：
   - 若 %K 低于 25，且前两根 K 线的 %K 斜率为正（即 %K[1] − %K[2] > 0），则开多。
   - 若 %K 高于 75，且斜率为负，则开空。
5. 反向信号出现时，会发送足够的交易量来平掉旧仓位并建立新的 `TradeVolume` 方向仓位。
6. 浮动盈利超过 `MinimumProfit` 时立即全部平仓。

## 仓位与恢复机制
- 初始下单量为 `TradeVolume`。
- 每次平仓后检查实现盈亏：
  - **亏损**：下一笔订单量乘以 2（复刻原策略的加倍手逻辑）。
  - **盈利或持平**：下次订单恢复为基础手数。

## 实现要点
- 随机指标与标准差通过高层 API `BindEx` 获取，无需手动复制指标缓冲区。
- 滑动窗口会根据 K 线的时间框架把日历天数转换为条数。
- 浮动盈利控制基于当前 K 线收盘价与 `PositionAvgPrice` 计算，等效于原始 MQL 版本仅统计未平仓收益的方式。
- 按要求仅提供 C# 版本，代码注释全部为英文。
