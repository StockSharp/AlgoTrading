# Hard Profit

## 概述
Hard Profit 是对 MetaTrader 4 专家顾问 `hardprofit.mq4` 的 StockSharp 移植版本。策略试图在价格收于蜡烛极值且平滑
趋势滤波确认方向时捕捉突破。移植过程中使用 StockSharp 的高级 API 重建了原始 EA 的资金管理、分批止盈以及止损
管理逻辑。

## 策略逻辑
### 突破结构
* 监控所选时间框架的收盘蜡烛，维护过去 `Breakout Period` 根蜡烛（不含当前蜡烛）的最高价与最低价，复现
  `iHighest`/`iLowest` 带偏移的行为。
* 使用中价驱动长度为 `Trend Period` 的平滑移动平均，其当前值与上一值的差值充当方向过滤器。

### 入场规则
* **做多入场** 需要满足：
  * 蜡烛收盘价等于最高价且向上突破前高区间；
  * 平滑均线斜率为正；
  * 当前无持仓且未达到每根蜡烛的入场次数限制；
  * 同时存在买一卖一报价且点差小于 `Max Spread (pips)`；
  * 未开启 `Only Short`（仅做空）限制。
* **做空入场** 完全对称：收于最低价并跌破前低区间、均线斜率为负、满足点差过滤，且 `Only Long` 未启用。

### 离场与风控
* 固定止损（`Stop Loss (pips)`）与可选止盈（`Take Profit (pips)`）形成外部保护区间。
* 浮动盈利达到 `Break-even (pips)` 时，止损上移至开仓价；盈利达到 `Trailing Activation (pips)` 时，止损再向前
  推进一个止损距离，锁定收益。
* 两个分批止盈沿用原始 EA 的比例：
  * `Partial TP1 (pips)` 达成时，平掉 `Partial Ratio 1 (%)` 的仓位；
  * `Partial TP2 (pips)` 达成时，在剩余仓位基础上平掉 `Partial Ratio 2 (%)`；
  分批量以当前仓位为基准计算，因此第二次分批会根据第一次后的剩余量自动缩放。
* 止损与止盈基于蜡烛内最高/最低价触发：做多时只要最低价触碰止损或最高价触及止盈即离场，做空则反向处理。

### 资金管理
移植了五种原有的资金管理模式，并结合 StockSharp 的组合数据做出调整：
1. **Fixed** – 每次入场使用 `Fixed Volume`。
2. **Geometrical** – 与账户规模平方根成正比：`0.1 * sqrt(balance / 1000) * Geometrical Factor`。
3. **Proportional** – 按照最新收盘价分配权益风险：`equity * Risk Percent / (price * 1000)`。
4. **Smart** – 基于 Proportional 结果，如果出现超过一次连续亏损，则按 `Decrease Factor` 减小下次仓位。
5. **TSSF** – 复刻 Triggered Smart Safe-Factor 逻辑，从最近 `Last Trades` 笔已实现收益中计算平均盈利、平均亏损
   与胜率，根据 `TSSF Trigger` 与 `TSSF Ratio` 调整风险权重；若条件恶化则回退到 0.1 手的最小值。最终仓位会按
   品种的 `VolumeStep`、`MinVolume`、`MaxVolume` 约束归一化。

## 参数说明
* **Breakout Period** – 计算突破高/低所需的历史蜡烛数量。
* **Trend Period** – 平滑移动平均的周期。
* **Only Short / Only Long** – 方向开关，用于禁用另一方向的入场。
* **Max Trades Per Bar** – 单根蜡烛内允许的入场次数（0 表示不限制）。
* **Stop Loss (pips)** – 初始止损距离，0 表示禁用。
* **Break-even (pips)** – 启动保本止损所需的盈利距离。
* **Trailing Activation (pips)** – 启动进阶止损（盈利追踪）的阈值。
* **Partial TP1 / Ratio 1** – 第一次分批止盈的距离与百分比。
* **Partial TP2 / Ratio 2** – 第二次分批止盈的距离与百分比。
* **Take Profit (pips)** – 最终止盈距离，0 表示无固定止盈。
* **Max Spread (pips)** – 入场时允许的最大点差。
* **Money Management** – 选择资金管理模式（Fixed、Geometrical、Proportional、Smart、TSSF）。
* **Fixed Volume** – Fixed 模式下的基础手数。
* **Geometrical Factor** – 几何模式中的放大因子。
* **Risk Percent** – Proportional、Smart、TSSF 模式使用的权益风险百分比。
* **Last Trades** – 记录用于自适应风控的历史交易数量。
* **Decrease Factor** – Smart 模式在连亏时的缩量因子。
* **TSSF Trigger 1/2/3 & TSSF Ratio 1/2/3** – TSSF 指标的阈值与风险权重。
* **Candle Type** – 驱动指标与信号的主时间框架。

## 补充说明
* Pip 大小由证券的价格步长推导，五位或三位外汇品种会自动映射为 10 个最小报价点。
* 分批止盈不会重置每根蜡烛的入场计数，保持与原始 EA 相同的处理方式。
* 资金管理统计基于已实现盈亏的增量计算，因此在 StockSharp 环境下完成第一笔平仓后才会逐渐生效。
* 当缺少买一卖一报价时，点差过滤自动失效，与原始 EA 在经纪商返回零点差时的行为一致。
