# Exp Cronex MFI 策略

## 概述
本策略复刻了 **Exp_CronexMFI** 智能交易系统。它对资金流量指标（MFI）进行两次平滑处理，并在两条线发生交叉时采取**逆势**交易。移植版本保持了原始的反转思想，并将所有参数暴露为 StockSharp 策略参数。

## 工作流程
1. 订阅所选蜡烛序列（默认使用 4 小时周期）。
2. 按照设定周期计算 Money Flow Index。
3. 使用指定的平滑方法进行两级处理：第一次得到快速 Cronex 线，第二次对快速线再次平滑生成慢速线。
4. 根据 `SignalShift` 保存历史快/慢线组合，模仿 MQL 中的 `SignalBar` 延迟。
5. 当快速线从上向下穿越慢速线时，关闭空头（若允许）并开立/加仓多头；当快速线从下向上穿越慢速线时，关闭多头并开立/加仓空头。
6. 所有订单均以策略 `Volume` 的数量按市价发送，并可分别禁用多头或空头方向。

策略仅在蜡烛收盘后评估信号，以与 MetaTrader 版本保持一致。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `MfiPeriod` | `int` | `25` | Money Flow Index 的周期长度。 |
| `FastPeriod` | `int` | `14` | 第一次平滑（快速线）的周期。 |
| `SlowPeriod` | `int` | `25` | 第二次平滑（慢速线）的周期。 |
| `SignalShift` | `int` | `1` | 等待的已完成蜡烛数量，用于延迟信号。 |
| `Smoothing` | `SmoothingMethod` | `Simple` | 两个平滑阶段使用的移动平均算法。 |
| `EnableLongEntries` | `bool` | `true` | 允许开立或加仓多头头寸。 |
| `EnableShortEntries` | `bool` | `true` | 允许开立或加仓空头头寸。 |
| `EnableLongExits` | `bool` | `true` | 允许信号平掉已有多头。 |
| `EnableShortExits` | `bool` | `true` | 允许信号平掉已有空头。 |
| `CandleType` | `DataType` | `TimeFrame(4h)` | 用于计算的蜡烛类型。 |
| `Volume` | `decimal` | `1` | 开仓时使用的下单数量。 |

## 平滑方式
原指标包含多个自定义模式，移植版本将其映射到内置移动平均：

| MQL 模式 | `SmoothingMethod` 取值 | 说明 |
| --- | --- | --- |
| SMA | `Simple` | 简单移动平均。 |
| EMA | `Exponential` | 指数移动平均。 |
| SMMA | `Smoothed` | 平滑移动平均（Wilder）。 |
| LWMA | `Weighted` | 线性加权平均。 |
| JJMA / JurX / ParMA / T3 / VIDYA / AMA | `DoubleExponential`, `TripleExponential`, `Hull`, `ZeroLagExponential`, `ArnaudLegoux`, `KaufmanAdaptive` | 选择最接近的自适应平滑方式。 |

## 与 MQL 版本的差异
- 无法在蜡烛层面区分真实与勾选成交量，使用的是 StockSharp 蜡烛提供的总量数据。
- 仅使用市价单进行仓位管理；原策略的延迟通过 `SignalShift` 参数模拟。
- 止损和止盈需要通过额外的风险控制或保护模块来设置。

## 使用建议
- 根据标的的流动性选择适当的蜡烛周期；默认的 4 小时与源策略一致。
- 当需要额外确认时，可提高 `SignalShift` 的取值。
- 建议结合 `StartProtection` 或其他风控机制限制潜在亏损。
