# Blau TStoch 指标策略

## 概述
- 将 MetaTrader 5 专家顾问 `Exp_BlauTStochI` 移植到 StockSharp 高级 API。
- 在可配置周期上交易 William Blau 的三重随机指数。
- 支持两种交易模式：**Breakdown**（穿越零轴）和 **Twist**（斜率反转）。
- 独立的开仓/平仓许可与原始 EA 完全一致，可分别控制多头与空头的进出。

## 指标构建
- 计算动量序列：`应用价格 - 最低价`（窗口长度 `MomentumLength`），以及范围 `最高价 - 最低价`。
- 对动量与范围分别执行三层连续平滑。
- 支持的平滑算法：指数 (EMA)、简单 (SMA)、平滑/RMA (SMMA) 和线性加权 (LWMA)。
- 原始 MQL 中的高级方法（JJMA、JurX、ParMA、T3、VIDYA、AMA）未实现；`Phase` 参数仅保留兼容性并被忽略。
- 应用价格选项等同于 MQL：收盘、开盘、最高、最低、中值、典型价、加权价、简单价、四分价、两个 TrendFollow 变体以及 DeMark 价格。
- 指标输出公式：`100 * 平滑动量 / 平滑范围 - 50`。

## 交易规则
### Breakdown 模式
- 使用 `SignalBar` 指定的已完成 K 线（默认 1，对应最近闭合的蜡烛）。
- **做多入场：** 前一值（`SignalBar+1`）大于 0，当前值（`SignalBar`）跌破或等于 0。
- **做空入场：** 前一值小于 0，当前值上穿或等于 0。
- **平多：** 前一值小于 0 且允许平多。
- **平空：** 前一值大于 0 且允许平空。

### Twist 模式
- **做多入场：** 指标上升（`value[SignalBar+1] < value[SignalBar+2]`），且最新值不低于上一值。
- **做空入场：** 指标下降（`value[SignalBar+1] > value[SignalBar+2]`），且最新值不高于上一值。
- **平多：** 指标斜率转为向下（`value[SignalBar+1] > value[SignalBar+2]`）。
- **平空：** 指标斜率转为向上（`value[SignalBar+1] < value[SignalBar+2]`）。

### 仓位管理
- 开仓时在设定 `Volume` 基础上加上反向仓位的绝对值，实现即时反手。
- 平仓指令一次性关闭全部仓位，使用市价单。
- 所有逻辑均在蜡烛收盘且指标完全形成后执行。

## 风险控制
- `StopLossPoints` 与 `TakeProfitPoints` 以价格步长为单位控制止损/止盈，可设置为 0 以关闭。
- 使用 `StartProtection` 自动设置保护性止损止盈。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 计算所用的数据类型/周期。 | 4 小时蜡烛 |
| `Smoothing` | 平滑方式 (EMA/SMA/SMMA/LWMA)。 | EMA |
| `MomentumLength` | 最高/最低窗口长度。 | 20 |
| `FirstSmoothing` | 第一层平滑长度。 | 5 |
| `SecondSmoothing` | 第二层平滑长度。 | 8 |
| `ThirdSmoothing` | 第三层平滑长度。 | 3 |
| `Phase` | 兼容性参数（未使用）。 | 15 |
| `PriceType` | 应用价格类型。 | Close |
| `SignalBar` | 信号所用的历史偏移（>=1）。 | 1 |
| `Mode` | 交易模式（Breakdown/Twist）。 | Twist |
| `AllowLongEntries` | 允许开多。 | true |
| `AllowShortEntries` | 允许开空。 | true |
| `AllowLongExits` | 允许平多。 | true |
| `AllowShortExits` | 允许平空。 | true |
| `TakeProfitPoints` | 止盈距离（价格步长，0 关闭）。 | 2000 |
| `StopLossPoints` | 止损距离（价格步长，0 关闭）。 | 1000 |

## 与 MT5 EA 的差异
- 仅实现基础平滑算法，其余 SmoothAlgorithms.mqh 中的扩展方法未移植。
- 仓位管理简化为使用 StockSharp 的 `Volume` 参数，不再支持原始 MM 逻辑。
- 只在收盘价信号上操作，不执行盘中信号。

## 使用建议
- 请保持 `SignalBar` ≥ 1，历史缓冲会自动维护所需长度。
- 增大平滑长度会显著延长指标形成时间，需相应调整等待周期。
- 在高周期反转策略中建议放宽止损/止盈或通过权限关闭某一方向以降低噪声。

