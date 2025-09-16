# Blau Ergodic MDI 策略

## 概述
Blau Ergodic Market Directional Indicator（MDI）策略完整复刻了 MetaTrader 智能交易系统 `Exp_BlauErgodicMDI` 的逻辑。策略读取较高时间周期的蜡烛图（默认 4 小时），对选定的价格源执行三次平滑处理，从而构建动量直方图和信号线。依据所选的入场模式，策略会在以下三种情形中触发信号：

1. **Breakdown** – 直方图穿越零轴时交易。
2. **Twist** – 直方图斜率发生反转时交易。
3. **CloudTwist** – 直方图与信号线发生交叉时交易。

每一次信号都可以根据权限设置选择性地平掉反向仓位并开立新的头寸。

## 指标流程
1. 使用指定的移动平均类型与 `PrimaryLength` 对所选价格进行平滑，得到基准价格。
2. 计算动量差值 `(price - baseline) / point_value`。
3. 使用 `FirstSmoothingLength` 与 `SecondSmoothingLength` 对动量进行两次平滑，形成直方图。
4. 再次以 `SignalLength` 平滑直方图，得到信号线。
5. 按照 `SignalBarShift` 缓存历史数值，确保只在已收盘的蜡烛上确认信号。

支持的平滑类型包括 **EMA**、**SMA**、**SMMA/RMA** 与 **WMA**。价格源的选择与原始 MQL 指标完全一致（收盘价、开盘价、最高价、最低价、中位价、典型价、加权价、简单价、四分价、趋势跟随价等）。

## 主要参数
| 参数 | 说明 |
| ---- | ---- |
| `Volume` | 开仓使用的下单数量。 |
| `StopLossPoints` | 以点数表示的止损距离（0 表示禁用）。 |
| `TakeProfitPoints` | 以点数表示的止盈距离（0 表示禁用）。 |
| `SlippagePoints` | 市价单允许的最大滑点（点数）。 |
| `AllowLongEntries` / `AllowShortEntries` | 是否允许开多 / 开空。 |
| `AllowLongExits` / `AllowShortExits` | 是否允许在反向信号出现时平仓。 |
| `Mode` | 入场模式（Breakdown / Twist / CloudTwist）。 |
| `CandleType` | 计算所用蜡烛图的时间框架（默认 4 小时）。 |
| `SmoothingMethod` | 各个平滑步骤使用的移动平均类型。 |
| `PrimaryLength` | 基准价格的平滑周期。 |
| `FirstSmoothingLength` | 动量第一次平滑的周期。 |
| `SecondSmoothingLength` | 构建直方图的第二次平滑周期。 |
| `SignalLength` | 生成信号线的平滑周期。 |
| `AppliedPrice` | 指标计算所使用的价格类型。 |
| `SignalBarShift` | 回溯确认信号的已收盘蜡烛数量。 |
| `Phase` | 为兼容原始脚本保留的参数，当前实现未使用。 |

## 信号判定
* **Breakdown**
  * 多头：`SignalBarShift` 对应的直方图大于 0，且前一根直方图不大于 0。
  * 空头：`SignalBarShift` 对应的直方图小于 0，且前一根直方图不小于 0。
* **Twist**
  * 多头：直方图先下降后回升（前一值 < 当前值，且前两根 > 前一根）。
  * 空头：直方图先上升后回落（前一值 > 当前值，且前两根 < 前一根）。
* **CloudTwist**
  * 多头：直方图向上穿越信号线（当前直方图 > 当前信号线，前一根直方图 ≤ 前一根信号线）。
  * 空头：直方图向下穿越信号线。

若允许平仓，信号会先平掉反向仓位；随后根据权限再开立新仓。

## 风险控制
策略调用 `StartProtection`，按照交易品种的最小报价单位（tick size）将止损、止盈和滑点的点数转换成价格距离。当对应参数为 0 时，保护不会启用。

## 备注
* 仅在蜡烛收盘后处理信号，与 MetaTrader 实现保持一致。
* 通过调整 `SignalBarShift` 可以延后信号确认，避免使用最新未确认的柱线。
* 为保持兼容性保留 `Phase` 参数，当前平滑类型下不会生效。
* 代码中的注释全部使用英文，方便跨团队维护。
