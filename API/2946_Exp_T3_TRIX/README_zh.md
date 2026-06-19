# Exp T3 TRIX 策略 (ID 2946)

Exp T3 TRIX 策略移植自 MetaTrader 5 的同名专家顾问，通过三重平滑的 TRIX 指标捕捉动量反转。策略利用 Tillson T3 平滑获得快慢两条 TRIX 曲线，并根据三种可选模式判定何时进出场。

## 交易逻辑

- **Tillson T3 TRIX 计算**
  - 为快线和慢线分别构建 6 层指数移动平均，得到 Tillson T3 数值。
  - 每条 T3 的导数（当前值与前值之差再除以前值）构成 TRIX 柱状图，作为信号源。
- **模式 = Breakdown**
  - *做多*: 快速 TRIX 从零轴下方上穿零轴且允许做多时触发。若允许平空，会先平掉已有空头。
  - *做空*: 快速 TRIX 从零轴上方下穿零轴且允许做空时触发。若允许平多，会先平掉已有多头。
  - *仅平仓*: 若出现穿越但相应入场被禁用，仍会在许可的情况下平掉对向持仓。
- **模式 = Twist**
  - *做多*: 快速 TRIX 的斜率由负转正（即先下行后上行）。其他平仓与权限规则与 Breakdown 相同。
  - *做空*: 快速 TRIX 的斜率由正转负。
- **模式 = CloudTwist**
  - *做多*: 快速 TRIX 在上一根收盘还位于慢速 TRIX 下方，本根收盘已上穿慢速 TRIX。
  - *做空*: 快速 TRIX 在上一根收盘还位于慢速 TRIX 上方，本根收盘已下穿慢速 TRIX。
- **下单处理**
  - 出现反向信号且允许平仓时，策略会优先关闭相反方向的持仓。
  - 新开仓的数量为 `Volume + |Position|`，允许在一次指令中完成反手。
  - 启用 `StartProtection()`，沿用 StockSharp 模板提供的保护机制。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Fast Length` | 10 | 快速 Tillson T3 的深度（6 层 EMA 链）。 |
| `Slow Length` | 18 | 慢速 Tillson T3 的深度。 |
| `Volume Factor` | 0.7 | Tillson T3 平滑系数 (0 ~ 1)。 |
| `Mode` | Twist | Breakdown、Twist、CloudTwist 三种信号模式。 |
| `Allow Long Entry` | true | 是否允许开多。 |
| `Allow Short Entry` | true | 是否允许开空。 |
| `Allow Long Exit` | true | 是否允许平多。 |
| `Allow Short Exit` | true | 是否允许平空。 |
| `Candle Type` | 4 小时时间框架 | 计算所用的蜡烛聚合周期。 |

所有参数都通过 `StrategyParam<T>` 暴露，可在 Designer 中显示并用于优化。

## 使用提示

1. 策略仅处理收盘完结的蜡烛，请确保数据源提供与 `Candle Type` 一致的时间框架。
2. 由于 TRIX 导数需要历史数据，最初两根完成的蜡烛只用于初始化，不会产生信号。
3. 想要复制原版的单向交易或禁止平仓，可关闭相应的 `Allow ...` 开关。
4. 原专家顾问没有内置止损或止盈，本策略同样未实现。需要风险控制时可结合 StockSharp 的资金管理模块。

## 移植细节

- 来源：`MQL/2156/exp_t3_trix.mq5` 以及指标 `t3_trix.mq5`。
- 移植版保持三种信号模式，并使用 StockSharp 的高阶蜡烛订阅及指标类。
- Tillson T3 通过 6 层 EMA 与可调的 `Volume Factor` 重建，默认系数 0.7。
