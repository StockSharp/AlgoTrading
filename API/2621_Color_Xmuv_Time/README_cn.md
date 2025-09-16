# Color XMUV 时间过滤策略

该策略将 MetaTrader 专家顾问 **Exp_ColorXMUV_Tm** 迁移到 StockSharp 平台，保持原始的 Color XMUV 平滑线与交易时段过滤
逻辑，同时利用 StockSharp 的高级 API 完成下单与风控。策略监控平滑线的颜色变化：当颜色翻转为向上（青色）时管理多头，
翻转为向下（洋红色）时管理空头。

## 核心流程
- 在每根完成的蜡烛上构造与 MQL 版本一致的复合价格：上涨 K 线使用 `(High + Close)/2`，下跌 K 线使用 `(Low + Close)/2`，
  平头 K 线使用 `Close`。
- 将复合价格输入指定的平滑算法。常见算法（SMA、EMA、SMMA/RMA、LWMA、Jurik）直接调用 StockSharp 指标实现；对于
  StockSharp 尚未提供的算法（例如 T3、VIDYA、ParMA）采用 EMA 作为替代，并保留 `Phase` 参数以兼容原始配置。
- 比较当前平滑值与上一根的平滑值，重建 Color XMUV 的颜色：上升斜率映射为看多颜色，下降斜率映射为看空颜色，其余为
  中性颜色。
- `SignalBar` 控制信号延迟的柱数。默认值 1 表示使用上一根已完成的柱体作为确认，避免实时柱产生噪音。
- 当颜色由非多头转为多头时，策略根据设置先平掉空头，再决定是否建立或加仓多头；颜色翻为看空时执行对称操作。
- 交易时段过滤完全复刻原始 EA：在指定时段之外立即平掉持仓并忽略新的入场信号，支持跨越午夜的交易窗口（起始时间大于
  结束时间时自动跨日）。
- `StopLossPoints` 与 `TakeProfitPoints` 表示以点数计的风险距离，策略会结合标的的最小报价步长转换为绝对价格，并通过
  `StartProtection` 注册，方便由 StockSharp 自动维护保护单。

## 风险与仓位管理
- `OrderVolume` 定义基本开仓手数。当方向反转时会自动在原有仓位基础上加上绝对值，实现一次市价单同时平旧仓与开新仓。
- 可选的止损与止盈通过点数自动换算为价格差。将参数设为 0 即可关闭对应保护。
- 颜色翻转触发的离场会遵循 `EnableBuyExits` 与 `EnableSellExits`，从而可以分别控制多头与空头的退出逻辑。

## 参数说明
- **Candle Type** – 计算所用的蜡烛类型（默认 4 小时蜡烛）。
- **Order Volume** – 基础市价单数量。
- **Enable Long Entries / Enable Short Entries** – 是否允许在多头/空头翻转时开仓。
- **Close Longs / Close Shorts** – 是否在颜色翻向相反方向时自动平仓。
- **Use Time Filter** – 是否启用交易时段过滤。
- **Start Hour / Start Minute / End Hour / End Minute** – 交易时段边界。若起始时间晚于结束时间，视为跨日会话。
- **Smoothing Method** – Color XMUV 平滑算法。若所选算法无原生实现，将自动使用 EMA 并在 README 中提示。
- **Length** – 平滑窗口长度（必须为正）。
- **Phase** – 为兼容原策略保留的相位参数，部分算法可能忽略此值。
- **Signal Bar** – 信号延迟的已完成柱数量，0 表示使用最新收盘柱。
- **Stop Loss (pts) / Take Profit (pts)** – 以点数表示的止损/止盈距离，0 表示禁用。

## 重要提示
- 原版指标依赖外部平滑算法库。在 StockSharp 中无法一一对应的算法（ParMA、VIDYA、T3 等）使用 EMA 近似，需要在使用
  时提前告知策略使用者。
- 为遵循仓库不构建冗余数据结构的要求，策略仅维护满足 `SignalBar` 需求的最小颜色历史。
- 按照需求，代码注释全部采用英文撰写。
