# The Predator 策略

## 概述

该策略是 MQL 专家顾问 **“The Predator”** 的 StockSharp 高级 API 版本。原策略通过趋势过滤、动量、布林带和随机指标的组合来寻找信号，并提供两个可选模板（Strategy 1 与 Strategy 2）。这些模板在移植中完整保留。

实现全部基于一条可配置的 K 线序列，通过 `SubscribeCandles` 订阅和指示器 `Bind/BindEx` 方式获得数据，无需手动维护历史缓存。

## 使用的指标

- **线性加权移动平均 (LWMA)**：快线与慢线评估趋势方向。
- **DMI + ADX**：衡量趋势强度与方向性。
- **Momentum（默认 14）**：计算价格相对 100 的偏离度。
- **布林带**：宽带与窄带共同判断前一根 K 线所处区间。
- **随机指标 (Stochastic)**：Strategy 2 的额外过滤条件。
- **MACD**：通过主线与信号线关系确认动量。

## 交易逻辑

### 通用规则

1. 仅处理已经收盘的 K 线。
2. 在交易前确认指标已经形成（`IsFormedAndOnlineAndAllowTrading`）。
3. ADX 必须高于指定阈值。
4. 维护最近三次 Momentum 偏差，用以模拟 MQL 中的多周期检查。

### Strategy 1

- **多头** 需要：
  - ADX > 阈值且 +DI > −DI。
  - 快速 LWMA 高于慢速 LWMA。
  - 最近三次 Momentum 偏差中至少一次超过买入阈值。
  - MACD 主线高于信号线。
- **空头** 条件相反。

### Strategy 2

- **多头** 额外要求：
  - 前一根 K 线收盘价位于窄带下轨或以上。
  - 随机指标的主线和信号线都高于上阈值。
  - 最近三次 Momentum 偏差中至少一次低于买入阈值（趋势回调）。
- **空头** 额外要求：
  - 前一根 K 线收盘价位于窄带上轨或以下。
  - 随机指标信号线高于上阈值，而主线低于下阈值。
  - 最近三次 Momentum 偏差中至少一次低于卖出阈值。

### 仓位管理

- 在进场前取消所有挂单。
- 当方向反转时，用组合市价单同时平掉现有仓位并建立反向仓位。

## 风险控制

- 通过 `StartProtection` 设置：
  - 止损距离（以点数为单位）。
  - 止盈距离（点）。
  - 可选的固定距离追踪止损。
- 点数会根据标的物的最小价格步长转换成绝对价格。
- 原 MQL 中的金额型止损、无损移动和通知系统未移植，使用了固定点数替代。

## 参数

| 参数 | 含义 |
|------|------|
| `Mode` | 选择 Strategy 1 或 Strategy 2。 |
| `FastMaLength`, `SlowMaLength` | LWMA 快线/慢线周期。 |
| `DmiPeriod`, `AdxSmoothing` | DMI 与 ADX 设置。 |
| `MomentumPeriod` | Momentum 周期。 |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | 接受信号所需的最小偏差。 |
| `AdxThreshold` | ADX 最低触发值。 |
| `BollingerPeriod`, `TightBandWidth`, `WideBandWidth` | 布林带参数。 |
| `StochasticLength`, `StochasticSmooth`, `StochasticUpper`, `StochasticLower` | 随机指标参数（Strategy 2）。 |
| `TradeVolume` | 交易数量。 |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | 风险距离（点）。 |
| `CandleType` | 使用的 K 线类型。 |

## 与 MQL 版本的差异

- 金额型止损/止盈/追踪转换为点数，通过 `StartProtection` 管理。
- 未实现原策略中的无损移动和邮件/推送通知功能。
- MQL 中部分指标调用了更高周期，这里仅使用单一订阅；若需要多周期可自行添加。
- 未实现阶梯加仓或马丁系数，StockSharp 版本采用固定 `TradeVolume`。

## 使用步骤

1. 按 StockSharp 示例建立连接与投资组合。
2. 实例化 `ThePredatorStrategy`，设置 `Security`、`Portfolio` 及参数。
3. 启动策略，可选地绑定图表以查看指标和成交。

该移植保留了原始决策逻辑，并结合 StockSharp 的绑定机制与保护模块，便于进一步优化和扩展。
