# ScalpWiz Bollinger 策略

## 概述

**ScalpWiz Bollinger** 是一款反趋势策略，利用布林带判断价格过度延伸。当收盘价远离上轨或下轨时，策略将在相反方向开仓，期待价格回归。

策略检查四个距离级别，每个级别代表不同的信号强度并相应地放大交易量。仓位大小还根据当前投资组合价值的风险百分比进行调整。

## 参数

- `BandsPeriod` – 计算布林带所使用的蜡烛数量。
- `BandsDeviation` – 布林带的标准差倍数。
- `Level1Pips` … `Level4Pips` – 触发各级别信号所需的点数距离。
- `StrengthLevel1Multiplier` … `StrengthLevel4Multiplier` – 各级别的交易量倍数。
- `RiskPercent` – 每个信号所承担的账户风险百分比。
- `CandleType` – 用于计算的蜡烛周期。

## 交易逻辑

1. 订阅所选周期的蜡烛并计算布林带。
2. 每根完成的蜡烛：
   - 如果收盘价高于上轨一定距离，则开空单；
   - 如果收盘价低于下轨一定距离，则开多单。
3. 交易量根据风险百分比和信号强度倍数计算。

该策略灵感来自原始的 MQL 脚本 `mcb.scalpwiz.9001.mq4`。

