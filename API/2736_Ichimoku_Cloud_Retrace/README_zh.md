# Ichimoku Cloud Retrace 策略
[English](README.md) | [Русский](README_ru.md)

本策略是 MetaTrader 专家顾问 “ichimok2005” 的 StockSharp 版本。它监控价格回落到 Ichimoku 云内部时的机会，并按照云层（kumo）的倾斜方向进场。所有判断均基于已完成的 K 线。

## 概览

- 适用于任何提供蜡烛图数据的品种和周期。
- 默认使用 Ichimoku 标准参数 9/26/52，所有周期均可调整。
- 支持做多和做空，仓位大小由策略的 `Volume` 属性决定。
- 可选的止损/止盈以绝对价格单位设置，填 0 表示关闭。

## 指标与参数

- **Ichimoku**：Tenkan、Kijun、Senkou Span B 的周期可分别配置。
- **蜡烛类型**：可选择连接所支持的任意聚合蜡烛类型（默认 1 小时）。
- **Stop Loss Offset**：距离开仓价的止损幅度，单位为价格，0 表示禁用。
- **Take Profit Offset**：距离开仓价的止盈幅度，单位为价格，0 表示禁用。

## 入场规则

### 做多条件

1. `Senkou Span A` 高于 `Senkou Span B`，表明云层为多头结构。
2. 当前完成的蜡烛为阳线（`Close > Open`）。
3. 收盘价位于云层内部（在两条 Span 之间）。
4. 条件满足且当前无多头持仓时，策略发送市价买单，同时覆盖任意空头敞口并建立新的多头。

### 做空条件

1. `Senkou Span B` 高于 `Senkou Span A`，表明云层为空头结构。
2. 当前完成的蜡烛为阴线（`Open > Close`）。
3. 收盘价位于云层内部（在两条 Span 之间）。
4. 条件满足且当前无空头持仓时，策略发送市价卖单，同时平掉多头并建立新的空头。

## 离场规则

- 反向信号会以一笔市价单同时完成平仓与反向开仓。
- 启用 `Stop Loss Offset` 后，多单在 `EntryPrice - Offset` 平仓，空单在 `EntryPrice + Offset` 平仓（依据蜡烛收盘价）。
- 启用 `Take Profit Offset` 后，多单在 `EntryPrice + Offset` 平仓，空单在 `EntryPrice - Offset` 平仓。
- 手动停止策略会清空内部记录的入场价格。

## 风险管理提示

- 偏移量以绝对价格表示，若使用点数/跳动点，请先换算成价格。
- 因为风险检查基于收盘价，短周期时建议使用较小的偏移量。
- 策略始终一次性平掉全部仓位，不包含分批或跟踪止损机制。

## 其他实现细节

- 通过高阶 API 订阅蜡烛，并使用 `BindEx` 将 Ichimoku 指标绑定到订阅。
- 仅处理状态为 `Finished` 的蜡烛，忽略过程中更新。
- 如果终端支持图表，会自动绘制价格、Ichimoku 云层以及成交记录。
- `ManageRisk` 在信号判定前执行，以确保保护性离场拥有更高优先级。
