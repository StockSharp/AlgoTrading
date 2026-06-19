# DynamicRS_C 策略
[English](README.md) | [Русский](README_ru.md)

本策略将 MetaTrader 顾问 **Exp_DynamicRS_C** 移植到 StockSharp 高级 API。它跟踪自定义 DynamicRS_C 指标的颜色变化，以识别动态支撑与阻力。当指标准线变为洋红色（颜色索引 `0`）时偏多，而转为蓝紫色（颜色索引 `2`）时偏空。移植版保留原始机器人的信号节奏、权限开关以及止损/止盈结构。

## 细节

- **入场条件**：
  - **多头**：由 `SignalBar` 指定的已完成 K 线将指标颜色从非 `0` 变为 `0`。策略会在允许 `AllowSellExit` 时平掉已有空单，然后在 `AllowBuyEntry` 为真时开多。
  - **空头**：信号柱将指标颜色从非 `2` 变为 `2`。策略会在 `AllowBuyExit` 允许时平掉多单，并在 `AllowSellEntry` 为真时开空。
- **方向**：支持双向交易，并可独立启用/禁用多、空入场和离场。
- **离场条件**：
  - 多单在出现空头信号且启用 `AllowBuyExit` 时平仓，或在止损/止盈触发时离场。
  - 空单在出现多头信号且启用 `AllowSellExit` 时平仓，或在风险限制触发时离场。
- **止损**：`StopLossPoints` 与 `TakeProfitPoints` 为相对入场价的绝对价差，设为零即关闭该保护。
- **过滤器**：
  - `SignalBar` 规定向前回溯的已完成 K 线数量，用于检测颜色变化，对应原始的 `CopyBuffer(..., SignalBar, 2)` 调用。
  - `CandleType` 选择用于指标计算与交易逻辑的时间框架（默认 4 小时，与原顾问一致）。

## 参数

- `CandleType` – 策略处理的 K 线类型。
- `Length` – DynamicRS_C 指标比较高低点时的回溯长度，对应 MQL 中的 `Length`。
- `SignalBar` – 回溯的已收盘 K 线数，用于判定信号，与 EA 的 `SignalBar` 一致。
- `AllowBuyEntry` / `AllowSellEntry` – 是否允许在对应信号上开多/开空。
- `AllowBuyExit` / `AllowSellExit` – 是否允许在反向信号出现时平掉当前多/空单。
- `StopLossPoints` – 相对入场价的止损距离，正值代表多单向下、空单向上的止损线。
- `TakeProfitPoints` – 相对入场价的止盈距离，正值代表多单向上、空单向下的目标价差。
- `Volume` – 继承自 `Strategy.Volume` 的基础下单手数。出现反向信号时会自动加量以便同时平掉旧仓。

## 指标逻辑

内置的 `DynamicRsCIndicator` 精确还原了 MetaTrader 指标的颜色缓冲行为：

- 记录最近 `Length` 根 K 线的最高价和最低价，并跟踪上一根 K 线的数值。
- 当当前最高价低于上一根 K 线和 `Length` 根之前的最高价，且低于上一笔指标数值时，缓冲区切换到颜色 `0` 并把输出锁定在该高点。
- 当当前最低价高于上一根 K 线和 `Length` 根之前的最低价，且高于上一笔指标数值时，缓冲区切换到颜色 `2` 并把输出设置为该低点。
- 其他情况下指标保持上一数值，颜色 `1` 在趋势之间充当过渡状态，与原始算法一致。

通过 `BindEx` 绑定，策略能够同时获得指标的数值与颜色索引，从而精确复现原顾问的信号节奏与交易决策。
