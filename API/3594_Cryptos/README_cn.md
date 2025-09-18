# Cryptos 策略

## 概述

**Cryptos Strategy** 是 MetaTrader4 专家顾问 `cryptos.mq4` 的 StockSharp 版本。策略主要面向 ETH/USD，通过布林带与线性加权移动平均线（LWMA）的组合，在波动收缩后捕捉突破行情。算法会在可配置的蜡烛数量内追踪摆动高点与低点，并将得到的范围乘以系数来生成收益目标。

## 交易逻辑

1. **趋势识别**：当收盘价触及上轨时，策略进入做空偏好；当收盘价触及下轨时，策略切换为做多偏好，同时冻结当前的摆动高低点，停止自动更新。
2. **入场条件**：
   - 当收盘价跌破 LWMA、偏好为空且当前没有空头仓位时开空。
   - 当收盘价升破 LWMA、偏好为多且当前没有多头仓位时开多。
3. **区间投射**：使用自动或手动锁定的摆动高/低点与 LWMA 之间的距离（以跳动点计算），并乘以 take-profit 系数来确定利润目标与风险基础上的仓位规模。
4. **风险控制**：每笔交易都会设置止损与止盈。多头止损放在摆动低点下方，空头止损放在摆动高点上方。参数在每次入场时重新计算，并在主循环中强制执行。
5. **跟踪退出**：如果多头收盘价跌破下轨（或空头收盘价升破上轨），仓位立即平仓，以复制原始 EA 的保护逻辑。

## 参数

| 参数 | 说明 |
|------|------|
| `CandleType` | 所有指标使用的蜡烛数据类型。 |
| `BollingerPeriod`, `BollingerWidth` | 布林带的周期与标准差倍数。 |
| `MaPeriod` | 基于中位价的线性加权均线周期。 |
| `LookbackCandles` | 自动搜寻摆动高/低点的蜡烛数量。 |
| `TakeProfitRatio` | 交易 ETH/USD 时用于目标价的区间倍数。 |
| `AlternativeTakeProfitRatio` | 其他品种使用的区间倍数。 |
| `RiskPerTrade` | 每笔交易计划承担的风险金额（报价货币）。 |
| `ValueIndex`, `CryptoValueIndex` | 将风险金额转换为仓位体量的乘数，分别用于普通品种和加密货币。 |
| `MinVolume`, `MaxVolume` | 在对齐交易量步长后允许的最小/最大仓位。 |
| `MinRangeTicks` | 允许的最小区间（以跳动点计），避免得到零距离的保护位。 |
| `SpreadPoints` | 以跳动点计的手动价差；若有最优买卖价则自动推算。 |
| `GlobalTrend` | 手动方向：`1` 强制空头，`2` 强制多头，`0` 则自动判断。 |
| `AutoHighLow` | 启用时每根蜡烛都会更新摆动点；禁用后维持当前值直到新的布林带触发。 |
| `ManualBuyTrigger`, `ManualSellTrigger` | 设为 `true` 可立即触发多头或空头入场（执行后自动复位）。 |
| `SkipBuys`, `SkipSells` | 分别禁止新建多单或空单。 |

## 仓位计算

仓位遵循 MT4 公式：`volume = RiskPerTrade / rangeTicks * valueIndex`。结果会按照 `VolumeStep` 对齐，然后限制在 `MinVolume`/`MaxVolume` 以及交易所要求的范围内。

## 使用提示

- 启动时策略会检查组合资金。如果余额低于 `RiskPerTrade * 3`，交易将被禁用，并记录一条警告，与原始 EA 的安全检查保持一致。
- 手动触发与偏好控制可让交易者在实盘中与主观决策保持同步。
- 对 ETH/USD 自动使用 `CryptoValueIndex` 与 `TakeProfitRatio`；其他品种会切换到备用参数。
- 止损与止盈在策略内部监控，无需额外的保护模块。

