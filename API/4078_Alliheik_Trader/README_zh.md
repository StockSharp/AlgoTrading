# Alliheik Trader 策略
[English](README.md) | [Русский](README_ru.md)

本策略来自 MetaTrader 4 专家顾问 **alliheik.mq4** 的转换。它将双重平滑的 Heiken Ashi 蜡烛与前移的鳄鱼指标下颚线结合使用。当平滑后的 Heiken Ashi 缓冲区交叉时入场，离场依靠下颚过滤、固定止盈以及基于价格的移动止损。

## 交易逻辑

- **Heiken Ashi 构建**
  - 使用 `PreSmoothMethod` / `PreSmoothPeriod` 对原始开高低收数据进行第一次平滑。
  - 基于平滑后的价格构建标准 Heiken Ashi 蜡烛。
  - 根据蜡烛颜色重新排列高/低缓冲区（看涨保留低/高，看跌交换为高/低）。
  - 对条件缓冲区再次平滑（参数 `PostSmoothMethod` / `PostSmoothPeriod`），所得数值用于信号判断。
- **信号判定**
  - **做多**：当前下方缓冲区小于上方缓冲区，且上一根柱子的关系相反或相等。
  - **做空**：当前下方缓冲区大于上方缓冲区，且上一根柱子的关系相反或相等。
- **下颚过滤与移动止损**
  - 鳄鱼下颚是一条长度为 `JawsPeriod` 的均线，向前偏移 `JawsShift` 根柱子，并使用 `JawsPrice` 指定的价格源。
  - 只有当 `Close[6]`（向前第六根收盘价）穿越下颚后，策略才允许自动平仓。
  - 当 `Close[6]` 与下颚的距离达到 8 个点且价格反向穿越下颚时，立即平仓。
  - 若 `TrailingStopPoints` 大于 0，当 `Close[6]` 位于盈利方向时，止损价格会跟随其移动。
- **止损与止盈**
  - `StopLossPoints` 与 `TakeProfitPoints` 定义固定距离的止损、止盈（0 表示禁用）。
  - 当价格继续向盈利方向发展时，移动止损会覆盖原始止损位置。

## 默认参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | 计算所用时间框架。 |
| `JawsPeriod` | 144 | 鳄鱼下颚移动平均的长度。 |
| `JawsShift` | 8 | 下颚向前偏移的柱数。 |
| `JawsMethod` | Simple | 下颚所用均线类型（Simple/Exponential/Smoothed/Weighted）。 |
| `JawsPrice` | Close | 输入下颚的价格类型（收盘、开盘、最高、最低、中价、典型价、加权价）。 |
| `PreSmoothMethod` | Exponential | 第一次平滑 OHLC 的均线类型。 |
| `PreSmoothPeriod` | 21 | 第一次平滑的周期。 |
| `PostSmoothMethod` | Weighted | 第二次平滑 Heiken Ashi 缓冲区的均线类型。 |
| `PostSmoothPeriod` | 1 | 第二次平滑的周期（1 表示不改变数值）。 |
| `StopLossPoints` | 0 | 固定止损距离，单位为点。 |
| `TrailingStopPoints` | 0 | 基于 `Close[6]` 的移动止损距离。 |
| `TakeProfitPoints` | 225 | 固定止盈距离。 |
| `OrderVolume` | 0.1 | 每次下单的手数。 |

## 使用的指标

- 对开、高、低、收四个序列分别进行的平滑移动平均。
- 基于平滑价格重建的 Heiken Ashi 蜡烛。
- 对条件缓冲区进行的第二次平滑，用于生成买卖信号。
- 可配置类型、偏移和价格源的鳄鱼下颚均线。

## 进出场规则

- **做多入场**：下方缓冲区由上向下穿越上方缓冲区，同时上一根柱子尚未满足做多条件。
- **做多离场**：满足以下任意条件即平仓：
  - `Close[6]` 曾位于下颚之上，并在距离达到 ≥8 点后重新跌破下颚；
  - 达到 `TakeProfitPoints` 所设目标；
  - 触发固定止损或移动止损。
- **做空入场**：下方缓冲区由下向上穿越上方缓冲区，同时上一根柱子尚未满足做空条件。
- **做空离场**：满足以下任意条件即平仓：
  - `Close[6]` 曾位于下颚之下，并在距离达到 ≥8 点后重新上破下颚；
  - 达到 `TakeProfitPoints` 所设目标；
  - 触发固定止损或移动止损。

## 转换说明

- 通过记录最近一次成交的 K 线，只允许每根 K 线触发一次新交易，复现原版 `isOrderAllowed()` 的限制。
- 保护性止损/止盈在策略内部模拟，因为 StockSharp 无法依赖 MT4 服务器端订单。
- 为了复制 `iMA` 在 `ma_shift = JawsShift` 下的行为，策略保存了带偏移的下颚历史值。
- 计算全部使用 StockSharp 的高阶 API 与内建指标，满足仓库编码规范。

## 风险提示与使用建议

- 策略支持同一标的的双向交易。
- 更适合趋势行情，平滑后的 Heiken Ashi 能够过滤噪音并突出摆动。
- 请根据标的波动性调整 `TrailingStopPoints` 与 `TakeProfitPoints`。
- 正式使用前务必进行历史回测与模拟账户验证。
