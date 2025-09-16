# 专家级一目均衡表策略

## 概述

本策略基于原始的 MQL5 "Expert Ichimoku" 智能交易系统，使用 StockSharp 高层 API 重写。系统属于趋势跟随型策略，通过组合一目均衡表的多个组件、前一根 K 线的价格行为以及可选的马丁格尔仓位管理来寻找入场机会。

策略在设定的时间框架上仅对**已完成的 K 线**进行计算。多空信号互斥，策略始终保持单一净仓位：如需翻仓，会先平掉原方向头寸再开立新的反向仓位。指标所需数据全部来自订阅的蜡烛序列，无需额外数据源。

## 核心逻辑

### 指标配置

* **Tenkan-sen（转换线）**：用于捕捉快速均线的突破。
* **Kijun-sen（基准线）**：与转换线组合的慢速均线。
* **Senkou Span A/B（先行 A/B 线）**：使用上一根 K 线的数值确认价格是否位于云层之上或之下。
* **Chikou Span（迟行线）**：通过与历史价格比较确认动量突破。

默认参数与原始 EA 一致（9 / 26 / 52），用户可以自由调整。

### 入场规则

**多头条件**：

1. **动量触发**（满足其一）：
   * 最新一根完成的 K 线上，Tenkan-sen 上穿 Kijun-sen（Tenkan<sub>t-1</sub> ≤ Kijun<sub>t-1</sub> 且 Tenkan<sub>t</sub> > Kijun<sub>t</sub>），或
   * 迟行线突破历史收盘价（Chikou<sub>t-1</sub> ≤ Close<sub>t-11</sub> 且 Chikou<sub>t</sub> > Close<sub>t-10</sub>）；
2. **云层过滤**：当前收盘价高于上一根 K 线的先行 A/B 线，即价格完全在云层之上；
3. **价格行为过滤**：上一根 K 线为阳线（Close<sub>t-1</sub> > Open<sub>t-1</sub>）；
4. **仓位过滤**：当前不存在多头仓位。如有空头仓位，先市价平仓，再进入多头。

**空头条件**与上述规则完全对称：

1. Tenkan-sen 下穿 Kijun-sen，或迟行线跌破历史开盘价（Chikou<sub>t-1</sub> ≥ Open<sub>t-11</sub> 且 Chikou<sub>t</sub> < Open<sub>t-10</sub>）；
2. 当前收盘价低于上一根 K 线的先行 A/B 线（价格在云层之下）；
3. 上一根 K 线为阴线；
4. 如持有多头，先平多再开空。

### 仓位与马丁格尔

* 基础下单量等于策略的 `Volume` 属性。
* 若启用 **Use Martingale**，上一笔交易亏损时，下一次入场的下单量会翻倍；盈利或打平会将倍数重置。
* 实际下单量受 `Volume × Max Position Multiplier` 上限限制，对应原始 EA 中“最多持仓数量”的保护机制。

### 风险控制

* **固定止损 / 止盈**：以绝对价格偏移量设定。若收盘价触及止损或止盈，则立即市价平仓。
* **移动止损**：当 `Trailing Stop Offset` 和 `Trailing Step` 均大于 0 时，只有当价格较入场价至少上涨（或下跌）`offset + step` 后才会移动止损，完全复刻 EA 中的阶梯式追踪逻辑。
* 策略仅维护一个净仓位。平仓后会计算实际盈亏，用于决定下一个信号是否应用马丁格尔加仓。

## 参数说明

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| Tenkan Period | Tenkan-sen 长度。 | 9 |
| Kijun Period | Kijun-sen 长度。 | 26 |
| Senkou Span B Period | Senkou Span B 长度。 | 52 |
| Stop Loss Offset | 入场价与止损价之间的绝对距离，0 表示禁用。 | 0 |
| Take Profit Offset | 入场价与止盈价之间的绝对距离，0 表示禁用。 | 0 |
| Trailing Stop Offset | 移动止损的基础距离。 | 0 |
| Trailing Step | 每次上调移动止损所需的额外价格变动。 | 0 |
| Max Position Multiplier | 有效下单量的最大倍数（基于 `Volume`）。 | 5 |
| Use Martingale | 是否在亏损后翻倍下一次仓位。 | true |
| Candle Type | 用于计算的蜡烛类型/时间框架。 | 1 小时时间框架 |

## 实战提示

* 策略至少需要 12 根已完成 K 线后才能评估全部条件（迟行线比较会引用最远 11 根之前的价格）。
* 由于 StockSharp 使用净仓位模型，`Max Position Multiplier` 通过限制单次下单量来模拟原策略中的持仓数量上限。
* 当 `Trailing Stop Offset` 或 `Trailing Step` 为 0 时，移动止损功能自动关闭；两者都大于 0 时，只有当价格突破 `offset + step` 后才会收紧止损。
* 策略在日志中记录所有进出场事件，便于回测和复盘。

## 使用步骤

1. 在策略容器或可视化设计器中配置交易品种与所需的蜡烛时间框架。
2. 设置基础 `Volume`，并根据品种波动性将原 EA 里的“点数”转换为价格偏移量，填写到止损/止盈/移动止损参数中。
3. 启动策略。当指标累积到足够历史数据后，会在每根完成的 K 线上检查交叉与迟行线突破，并自动执行风控与马丁格尔逻辑。

