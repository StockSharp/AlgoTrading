# SilverTrend ColorJFatl Digit 策略
[English](README.md) | [Русский](README_ru.md)

## 概述

SilverTrend ColorJFatl Digit 策略将两个经典的 MQL 指标系统封装成一个 StockSharp 高级策略。SilverTrend 模块通过短周期高低价通道来识别突破方向；ColorJFatl Digit 模块则使用 Jurik 移动平均 (JMA) 对价格进行平滑处理，并分析其斜率的正负号。只有当两个模块同时指向同一方向时，策略才会开仓或持仓；一旦信号出现分歧，仓位立即平仓。

该实现遵循 `AGENTS.md` 的要求：使用 `SubscribeCandles().Bind(...)` 来连接指标，不直接访问内部缓冲区；延迟逻辑通过轻量队列完成；代码中提供了详细的英文注释，便于继续研究和二次开发。

## 策略逻辑

### 1. SilverTrend 突破模块

* 通过 `Highest` 和 `Lowest` 指标（窗口长度为 `SilverTrendLength + 1`）构建最近的价格通道。
* `SilverTrendRisk` 参数对通道进行收缩（原始公式 `33 - risk`），值越大，通道越窄，信号越敏感。
* 收盘价突破上阈值时认为出现多头趋势 (`+1`)，跌破下阈值时认为出现空头趋势 (`-1`)。
* `SilverTrendSignalBar` 设置信号确认所需的闭合 K 线数量，忠实还原原策略的 `SignalBar` 行为。

### 2. ColorJFatl Digit 确认模块

* 使用 `JurikMovingAverage` 对由 `JmaPriceType` 指定的价格进行平滑，支持 MetaTrader 的全部 applied price 选项（收盘价、开盘价、中值、典型价、趋势跟随价、Demark 等）。
* JMA 输出根据 `JmaRoundDigits` 四舍五入，模拟原始数字化输出。
* 圆整后的 JMA 斜率为正则输出 `+1`，为负输出 `-1`，若斜率为零则沿用上一状态，避免频繁抖动。
* `JmaSignalBar` 指定在动作前需要等待的闭合 K 线数量，实现与原指标相同的延迟确认。

### 3. 交易执行

* **入场：**
  * 当两个模块同时给出 `+1` 且当前没有多头仓位时买入。
  * 当两个模块同时给出 `-1` 且当前没有空头仓位时卖出。
* **离场：**
  * 两个信号出现分歧（包含返回 `0`）时立即平仓。
  * 方向反转时先平掉原有仓位，再开立新仓，避免加仓或摊平。
* 在方向切换前取消所有挂单，保持委托簿整洁。

## 参数说明

| 参数 | 说明 |
| --- | --- |
| `SilverTrendCandleType` | SilverTrend 通道使用的 K 线类型，默认等价于 H4。 |
| `SilverTrendLength` | 通道回溯长度（原策略中的 `SSP`）。 |
| `SilverTrendRisk` | 通道收缩因子 (`33 - risk`)，越大越灵敏但也更易假突破。 |
| `SilverTrendSignalBar` | 信号确认所需的闭合 K 线数量。 |
| `ColorJfatlCandleType` | JMA 模块使用的 K 线类型，可与 SilverTrend 不同。 |
| `JmaLength` | Jurik 移动平均的长度。 |
| `JmaSignalBar` | 在采纳 JMA 斜率前需要等待的闭合 K 线数。 |
| `JmaPriceType` | JMA 输入价格类型（close、open、median、TrendFollow 等）。 |
| `JmaRoundDigits` | 对 JMA 输出进行四舍五入时保留的小数位数。 |

## 实现细节

* 延迟逻辑通过固定长度的 FIFO 队列完成，不需要保存完整历史数据，符合高效内存使用的要求。
* 交易信号完全由高层 API 触发，未直接访问任何指标缓冲；这样更容易维护与测试。
* 代码对每一步计算都有英文注释，解释阈值调整、斜率判断、队列管理和仓位控制逻辑。
* 若运行环境提供图表，策略会绘制价格、SilverTrend 通道以及实际成交，方便监控与复盘。

## 使用建议

1. **适用市场与周期：** 原策略面向 H4 外汇图表。对于节奏较慢的商品或加密货币同样有效；若用于更快的周期，请谨慎降低 `SilverTrendLength` 与 `JmaLength`。
2. **联合优化：** 需要同时调整突破窗口与确认窗口（`SilverTrendLength` 与 `JmaLength`），单独修改某一侧容易造成信号不一致。
3. **Applied price 试验：** 在 Heikin-Ashi、Renko 等平滑图表上，TrendFollow 或 Demark 价格常常表现更稳定，可重点测试。
4. **风险控制：** 虽然双重确认能过滤不少噪声，但极端行情仍可能突破通道。建议结合账户级止损或基于 ATR 的 `StartProtection`。 
5. **头寸管理：** 策略默认使用 `Strategy.Volume` 作为下单数量。若需动态调仓，可结合 StockSharp 的资金管理组件。

## 拓展方向

* 在确认模块中引入更高周期（如日线）以构建多周期趋势过滤。
* 结合成交量或情绪指标（如 OBV、VWAP 偏离）进一步减少震荡区交易。
* 在累计测试后加入 `StartProtection`，设置 ATR 或百分比形式的止损/止盈。
