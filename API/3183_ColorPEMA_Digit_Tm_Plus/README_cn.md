# Exp Color PEMA Digit Tm Plus 策略

## 概述
**Exp Color PEMA Digit Tm Plus** 是 MetaTrader 5 智能交易系统 "Exp_ColorPEMA_Digit_Tm_Plus" 的移植版本。策略完整重建 Pentuple Exponential Moving Average (PEMA) 指标，并保留原始 EA 中的全部交易许可开关。只有当指标颜色发生翻转且等待条数 (`Signal Bar`) 条件满足时，才会在所选的蜡烛序列上执行交易。

StockSharp 版本继续提供原策略中的资金管理模式、止损/止盈控制和按时间强制离场功能。所有设置均通过 `StrategyParam<T>` 发布，便于在界面中配置和用于参数优化。

## 指标逻辑
* 使用八个串联的指数移动平均线，长度均为 `PEMA Length`，价格来源由 `Applied Price` 决定。
* 最终输出在写入历史前会按 `Rounding Digits` 进行四舍五入，与 MQL 指标完全一致。
* 通过计算相邻数值的斜率得到三种状态：
  * **Up (magenta)** —— 看涨，准备做多；
  * **Flat (gray)** —— 中性，无操作；
  * **Down (dodger blue)** —— 看跌，准备做空。
* 每根已完成蜡烛的状态都会被保存，以便在 `Signal Bar` > 0 时访问历史值。

## 交易规则
1. **信号识别**：在蜡烛收盘后读取 `Signal Bar` 条之前的指标状态，并与再早一条数据比较。
2. **多头条件**：当状态从其他值切换到 *Up* 时：
   * 若启用了 `Allow Long Entries`，则排队等待开多；
   * 若启用了 `Allow Short Exits`，则排队平掉已有空单。
3. **空头条件**：当状态从其他值切换到 *Down* 时：
   * 若启用了 `Allow Short Entries`，则排队等待开空；
   * 若启用了 `Allow Long Exits`，则排队平掉已有多单。
4. **执行层**：只有当策略在线、到达信号对应的激活时间且资金管理给出非零手数时，排队动作才会真正发送订单。
5. **风险控制**：
   * 止损和止盈按与 MT5 相同的点值距离基于成交价计算；
   * `Use Time Exit` 会在仓位持有超过 `Holding Minutes` 分钟后强制平仓；
   * 允许的反向信号可以立即平掉当前仓位。

## 参数说明
| 参数 | 说明 |
| ---- | ---- |
| Money Management | 资金管理基础数值。 |
| Money Mode | 资金管理模式（手数或按余额比例）。 |
| Stop Loss (points) | 止损距离，单位为点。 |
| Take Profit (points) | 止盈距离，单位为点。 |
| Allowed Deviation | 允许的报价偏差（为兼容而保留）。 |
| Allow Long/Short Entries | 是否允许开多/开空。 |
| Allow Long/Short Exits | 是否允许因反向信号平多/平空。 |
| Use Time Exit | 是否启用按时间离场。 |
| Holding Minutes | 最大持仓时间（分钟）。 |
| Candle Type | 处理的蜡烛类型（默认 H4）。 |
| PEMA Length | PEMA 链中每个 EMA 的长度。 |
| Applied Price | 指标使用的价格来源。 |
| Rounding Digits | 指标结果的小数位数。 |
| Signal Bar | 等待的已完成蜡烛数量。 |

## 使用提示
* 在 StockSharp 终端中将策略绑定到目标品种和所需的蜡烛序列。
* 根据原 EA 的设置调整参数。
* 策略仅在蜡烛完成后做出决策，因此行为与 MT5 逻辑保持一致。

## 转换状态
* **C# 版本** —— 已实现（`CS/ExpColorPemaDigitTmPlusStrategy.cs`）。
* **Python 版本** —— 未创建（按要求省略）。
