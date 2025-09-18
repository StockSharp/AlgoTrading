# MartingailExpert v1.0 Stochastic 策略（C#）

## 概述

**MartingailExpert v1.0 Stochastic** 策略将 MetaTrader 4 顾问 `MartingailExpert_v1_0_Stochastic.mq4`
完整迁移到 StockSharp 高级 API。策略读取随机指标的 %K 与 %D 线，并在上一根已结束 K 线的数值
满足阈值条件时开仓。首单建立后，会按照马丁格尔规则追加同向市场单，使最新成交价成为整
个仓位簇的统一止盈参考。

实现过程中使用了烛线订阅、`BindEx` 指标绑定以及 `BuyMarket`/`SellMarket` 等高层接口；源码
中的注释全部改为英文，并严格遵守项目要求的制表符缩进。

## 交易逻辑

### 1. 入场信号

1. 随机指标参数为 `Length = KPeriod`、`K.Length = Slowing`、`D.Length = DPeriod`，仅处理已完成的
   K 线。
2. 为了复现 MQL 函数 `iStochastic(..., shift = 1)`，策略缓存上一根柱子的 %K 与 %D 值。若
   `K_prev > D_prev` 且 `D_prev > ZoneBuy`，则开多；若 `K_prev < D_prev` 且 `D_prev < ZoneSell`，则开空。
3. 首次建仓使用 `BuyVolume` 或 `SellVolume`，并清除相反方向的马丁格尔状态，避免混合多空序列。

### 2. 马丁格尔加仓

1. 当 `_buyOrderCount` 或 `_sellOrderCount` 大于零时，策略监测烛线的最低价（多头）或最高价（空头）。
2. **加仓间距**
   * `StepMode = 0`：价格必须相对上一次成交逆向移动 `StepPoints × PointSize`。
   * `StepMode = 1`：采用 `StepPoints + max(0, 2 × ordersCount − 2)` 的点数距离，与 MQL 中
     `step + OrdersTotal*2 - 2` 的写法保持一致，并乘以根据 `Security.PriceStep` 推算的点值（对 3/5 位
     小数的外汇品种会额外乘以 10）。
3. 当触发价被穿越时，按照 `上一单手数 × Multiplier` 发送新的市价单。手数会根据 `VolumeStep`
   归一化，并在超出 `VolumeMax` 或低于 `VolumeMin` 时自动截断。
4. 每次加仓后，共用的止盈价会调整为
   `lastEntryPrice ± ProfitFactorPoints × PointSize × orderCount`，正负号由方向决定。

### 3. 止盈控制

1. 一旦烛线触及目标价（多头看 `High`，空头看 `Low`），策略会评估相对加权平均开仓价的收益，
   相当于原版 EA 中对 `OrderProfit()` 的正收益检查。
2. 若估算结果为正，就调用 `SellMarket(Math.Abs(Position))` 或 `BuyMarket(Math.Abs(Position))`
   平掉整组仓位，并重置所有马丁格尔状态。
3. 如果仓位被外部因素关闭（人工操作、爆仓等），下一根 `Position == 0` 的烛线会自动清除缓存，
   保持内部状态一致。

### 4. 其它实现细节

* 点值来源于 `Security.PriceStep`；若步长等于 `0.00001` 或 `0.001`，则乘以 10 来匹配 MetaTrader
  对 “Point” 的定义。
* 在 `OnStarted` 中调用一次 `StartProtection()`，以启用平台的标准保护机制。
* 策略会在独立图表区域绘制烛线、随机指标和自身成交，方便回测或实时监控。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| ---- | ---- | ------ | ---- |
| `StepPoints` | decimal | `25` | 价格逆向移动多少点后触发加仓。 |
| `StepMode` | int | `0` | `0`：固定距离；`1`：固定距离 + `2 × ordersCount − 2` 点。 |
| `ProfitFactorPoints` | decimal | `10` | 每张订单贡献的止盈点数，用于计算整组仓位的目标价。 |
| `Multiplier` | decimal | `1.5` | 每次加仓的手数乘数。 |
| `BuyVolume` | decimal | `0.01` | 首笔多单的手数。 |
| `SellVolume` | decimal | `0.01` | 首笔空单的手数。 |
| `KPeriod` | int | `200` | 随机指标 %K 的基础周期。 |
| `DPeriod` | int | `20` | %D 线的平滑周期。 |
| `Slowing` | int | `20` | %K 的附加平滑系数（MetaTrader `slowing` 参数）。 |
| `ZoneBuy` | decimal | `50` | 允许做多时 %D 必须高于的阈值。 |
| `ZoneSell` | decimal | `50` | 允许做空时 %D 必须低于的阈值。 |
| `CandleType` | `DataType` | `5 分钟` | 计算所使用的蜡烛类型。 |

## 目录结构

```
API/3991/
├── CS/
│   └── MartingailExpertV10StochasticStrategy.cs
├── README.md
├── README_cn.md
└── README_ru.md
```

根据任务要求，本目录暂不提供 Python 版本。
