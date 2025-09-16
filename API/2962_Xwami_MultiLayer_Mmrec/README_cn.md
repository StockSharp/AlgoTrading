# XWAMI 多层 MMRec 策略

该策略将 **Exp_XWAMI_NN3_MMRec.mq5** EA 迁移到 StockSharp。三个独立层（A/B/C）在不同周期上运行 XWAMI 动量指标，并把信号汇总成单一净头寸。每个层都重现原版对应 MagicNumber 的资金管理（MMRec）与防护设置。

## 交易逻辑

* 每个层以所选价格计算动量序列 `price - price[iPeriod]`，再经过四级平滑（方法与周期可单独设置），得到 XWAMI 的 `up` 与 `down` 线。
* 信号在 `SignalBar` 指定的历史柱上评估：若前一柱 `up > down`，该层会先平掉空单，并在最新柱出现 `up <= down` 时允许开多；若前一柱 `up < down`，则平掉多单，并在最新柱 `up >= down` 时允许开空。
* 为了适配 StockSharp 的净额模式，转向前会先关闭其他层的反向仓位，相当于在 MQL 中平掉相反 MagicNumber 的单子。
* 止损和止盈以价格点数设置，并在每根完结 K 线的最高/最低上检查；触发后立即退出该层仓位。

## 资金管理 (MMRec)

每个层维护自身的最近交易记录。如果在设定窗口内的亏损次数达到 *LossTrigger*，后续交易将使用 `SmallVolume`；当亏损不足该阈值时恢复到 `NormalVolume`。多头与空头各自独立统计，行为与原始 MMRec 模块一致。

## 参数

* `Layer?CandleType` — 层使用的 K 线类型（默认：A=8小时、B=4小时、C=1小时）。
* `Layer?Period` — 动量差分所用的滞后周期。
* `Layer?Method1..4`、`Layer?Length1..4`、`Layer?Phase1..4` — 四级平滑的方式、长度与相位。
* `Layer?AppliedPrice` — 计算动量所用的价格公式（收盘、开盘、加权、Demark 等）。
* `Layer?SignalBar` — 用于判定信号的柱索引。
* `Layer?AllowBuy/SellOpen`、`Layer?AllowBuy/SellClose` — 是否允许开/平多空。
* `Layer?NormalVolume`、`Layer?SmallVolume` — 正常与缩减模式的下单数量。
* `Layer?BuyTotalTrigger`、`Layer?BuyLossTrigger`、`Layer?SellTotalTrigger`、`Layer?SellLossTrigger` — MMRec 触发参数。
* `Layer?StopLossPoints`、`Layer?TakeProfitPoints` — 止损/止盈点数（0 表示禁用）。

## 说明

* 本实现使用单一净持仓，遇到矛盾信号时会先平反向仓位再入场，避免产生对冲。
* Tillson T3 平滑在 C# 中按原始公式实现；其余平滑方法映射到 StockSharp 内建指标（SMA、EMA、SMMA/RMA、LWMA、Jurik）。
* 由于平台差异，MMRec 的交易统计在策略内部维护，无需遍历终端历史即可保持原有阈值行为。
