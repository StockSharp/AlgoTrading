# Raymond Cloudy Day 策略

## 概述
Raymond Cloudy Day 是一套突破再入场策略，完整复现了原始 MQL5 专家 **“Raymond Cloudy Day for EA”** 的交易逻辑。算法通过更高周期蜡烛计算出一组关键参考价位，并在执行周期上利用这些价位寻找动量恢复信号。移植到 StockSharp 后，原有规则得到保留，同时每个模块都可以通过参数进行配置。

## 市场数据
- **信号蜡烛**：执行交易的周期。策略订阅该序列以产生进场信号并管理头寸。
- **枢轴蜡烛**：用于计算 Raymond 水平的高周期数据。默认是日线，对应 MQL5 输入参数 `RayMondTimeframe`。

通过 `GetWorkingSecurities` 自动注册上述两个订阅，策略启动时即可请求所需的数据流。

## Raymond 水平的计算
每当一根高周期蜡烛收盘，策略都会按以下公式更新 Raymond 水平：

\[
\begin{aligned}
TradeSS &= \frac{High + Low + Open + Close}{4} \\
PivotRange &= High - Low \\
ETB &= TradeSS + 0.382 \times PivotRange \\
ETS &= TradeSS - 0.382 \times PivotRange \\
TPB1 &= TradeSS + 0.618 \times PivotRange \\
TPS1 &= TradeSS - 0.618 \times PivotRange \\
TPB2 &= TradeSS + PivotRange \\
TPS2 &= TradeSS - PivotRange
\end{aligned}
\]

最新的计算结果会保存在策略字段中，并在每次更新时写入日志，方便跟踪水平随时间的变化。

## 入场规则
在获得 Raymond 水平后，策略会检查每一根完成的信号蜡烛：

1. **做多**：若蜡烛最低价跌破 `TPS1`，而收盘价重新站上该水平，则开多仓。这与 EA 条件 `Low[1] < TPS1 && Close[1] > TPS1` 完全一致，旨在捕捉对支撑位的反弹。
2. **做空**：若整根蜡烛保持在 `TPS1` 之上但最终收盘价跌破该水平，则开空仓（与原版相同的非对称规则）。

下单前策略会取消未成交订单，并在需要时平掉反向仓位，确保任意时刻只有一个方向的持仓。

## 风险控制
Raymond Cloudy Day 使用以 tick 为单位的对称保护带：

- **止损**：对于多头放在入场价下方 `ProtectiveOffsetTicks`；对于空头放在上方相同距离。
- **止盈**：与止损距离相同，但位于盈利方向。

偏移量乘以证券的 `PriceStep` 转换为绝对价格距离。每根信号蜡烛收盘后都会检查是否触发止损或止盈，如触发则立即平仓并重置内部保护变量。

## 参数
| 参数 | 说明 | 默认值 | 备注 |
|------|------|--------|------|
| `TradeVolume` | 每次进场使用的下单量。 | `1` | 启动时同步到策略的 `Volume` 属性。 |
| `ProtectiveOffsetTicks` | 止损与止盈的 tick 距离。 | `500` | 通过 `PriceStep` 转换成价格。 |
| `SignalCandleType` | 触发交易信号的蜡烛类型。 | 1 小时蜡烛 | 可选择任意蜡烛类型 (`DataType`)。 |
| `PivotCandleType` | 计算 Raymond 水平的高周期。 | 1 天蜡烛 | 对应 MQL EA 中的 `RayMondTimeframe`。 |

所有参数均提供优化区间和说明，便于在 StockSharp Designer 中配置。

## 其他说明
- 证券必须提供 `PriceStep`，否则无法计算保护价格，策略会跳过进场并记录警告。
- 图表绘制包含执行周期的蜡烛以及成交的交易，需要时可自行扩展显示内容。
- 实现仅处理收盘蜡烛，不直接轮询指标数值，完全遵循 `AGENTS.md` 中的开发规范。

## 原始 EA 保留的特性
- Raymond 水平的全部公式及系数（`0.382`、`0.618`、`1.0`）。
- 基于第一个卖出止盈水平 (`TPS1`) 的进场条件。
- 500 点对称止损/止盈，已转换为 StockSharp 环境下的 tick 偏移。

凭借这些要素，StockSharp 版本既复现了原始专家的行为，又提供了更灵活的配置与日志，便于后续研究和自动化。

