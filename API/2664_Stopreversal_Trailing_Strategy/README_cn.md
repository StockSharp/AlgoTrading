# Stopreversal Trailing 策略
[English](README.md) | [Русский](README_ru.md)

Stopreversal Trailing 策略复刻了 MT5 专家顾问 `Exp_Stopreversal.mq5`。它调用 Stopreversal 自定义指标，在所选的 K 线价格周围构建动态追踪止损线。当价格向上突破该追踪线时，被视为看涨反转，可选地平掉空头仓位并开多；向下突破则执行相反的看跌操作。为了与原始 EA 保持一致，可以通过参数将信号延后若干个已收盘的 K 线才执行。

## 细节

- **入场逻辑**：响应 Stopreversal 指标在价格穿越自适应追踪止损时产生的箭头信号。
- **多空方向**：同时支持多头与空头，并提供独立开仓开关。
- **出场逻辑**：反向 Stopreversal 信号可关闭当前仓位，同时可启用保护性的止损与止盈。
- **止损/止盈**：固定价格步长的止损、止盈，加上由指标触发的反转平仓。
- **数据来源**：任意时间框架；默认使用 4 小时 K 线，复现原始专家的多时间框架调用。
- **信号延迟**：`SignalBar` 参数会将下单延迟指定数量的已完成 K 线（默认 1 根）。
- **风险控制**：启动时调用仓位保护服务，并可使用按价格步长设置的硬止损。
- **指标参数**：`Npips` 控制价格与追踪线之间的距离；`PriceMode` 指定用于计算的价格类型。
- **默认值**：
  - `Volume` = 1
  - `StopLossSteps` = 1000
  - `TakeProfitSteps` = 2000
  - `BuyPositionOpen` = true
  - `SellPositionOpen` = true
  - `BuyPositionClose` = true
  - `SellPositionClose` = true
  - `Npips` = 0.004
  - `PriceMode` = Close
  - `SignalBar` = 1

## 参数

| 参数 | 说明 |
|------|------|
| `CandleType` | 用于 Stopreversal 计算和交易的 K 线类型，默认是 4 小时。 |
| `Volume` | 新建仓位时发送的基础下单量。 |
| `StopLossSteps` | 止损距离（价格步长数量），0 表示关闭。 |
| `TakeProfitSteps` | 止盈距离（价格步长数量），0 表示关闭。 |
| `BuyPositionOpen` | 看涨信号出现时是否允许开多。 |
| `SellPositionOpen` | 看跌信号出现时是否允许开空。 |
| `BuyPositionClose` | 看跌信号出现时是否平掉已有多头。 |
| `SellPositionClose` | 看涨信号出现时是否平掉已有空头。 |
| `Npips` | 调整追踪止损距离的比例系数。 |
| `PriceMode` | 所使用的价格类型（收盘价、开盘价、最高价、最低价、中位价、典型价、加权价、简单均价、四价平均、趋势跟随或 Demark）。 |
| `SignalBar` | 在执行信号前需等待的已收盘 K 线数量，对应 MT5 中的同名参数。 |

## 筛选信息

- **类别**：顺势反转
- **方向**：双向
- **指标**：Stopreversal（基于 ATR 的追踪止损）
- **止损**：固定止损与止盈，可选
- **时间框架**：可配置（默认 H4）
- **季节性**：无
- **神经网络**：无
- **背离**：无
- **复杂度**：中等（自定义追踪逻辑）
- **风险级别**：可通过止损距离和追踪参数调节
