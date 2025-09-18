# Elli Ichimoku ADX 策略

## 概述
该策略是 MetaTrader 5 专家顾问 “Elli”（barabashkakvn 修改版）的 StockSharp 移植版本。策略结合 Ichimoku 云结构和平均趋向指标（ADX）的 +DI 突破过滤，仅在价格动量足够强、并且 Ichimoku 关键线条呈现一致方向时入场。

在 StockSharp 中保留了双时间框架的设计：Ichimoku 分析默认在 1 小时 K 线完成后触发，ADX 则使用更快的 1 分钟数据流计算。下单使用市价，并附带以价格步长表示的固定止损和止盈，与原策略保持一致。

## 指标与数据
- **Ichimoku**：默认 Tenkan 19、Kijun 60、Senkou Span B 120。
- **平均趋向指数（ADX）**：仅使用 +DI 方向指标，与原版相同。
- 图表区域可绘制价格 K 线、Ichimoku 云以及 ADX 线条。

建立两条独立的订阅：
1. `IchimokuCandleType`（默认 1 小时）— 驱动 Ichimoku 计算及交易信号。
2. `AdxCandleType`（默认 1 分钟）— 供 ADX 计算并记录最新/上一条 +DI。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TakeProfitPoints` | 60 | 止盈距离（价格步长）。设为 0 表示不开启。 |
| `StopLossPoints` | 30 | 止损距离（价格步长）。设为 0 表示不开启。 |
| `TenkanPeriod` | 19 | Tenkan-sen（转换线）周期。 |
| `KijunPeriod` | 60 | Kijun-sen（基准线）周期。 |
| `SenkouSpanBPeriod` | 120 | Senkou Span B 周期。 |
| `AdxPeriod` | 10 | ADX 计算周期。 |
| `PlusDiHighThreshold` | 13 | 当前 +DI 必须突破的阈值。 |
| `PlusDiLowThreshold` | 6 | 上一条 +DI 必须低于的阈值。 |
| `BaselineDistanceThreshold` | 20 | Tenkan 与 Kijun 之间的最小距离（以价格步长计）。 |
| `IchimokuCandleType` | 1 小时 | Ichimoku 计算所用 K 线类型。 |
| `AdxCandleType` | 1 分钟 | ADX 计算所用 K 线类型。 |

## 交易逻辑
1. 等待 Ichimoku 订阅上的最新 K 线收盘。
2. 确认 ADX 已提供最近两条数据，并出现 +DI 突破：`上一条 +DI < PlusDiLowThreshold` 且 `当前 +DI > PlusDiHighThreshold`。
3. 将 Tenkan 与 Kijun 的差值换算为价格步长，确保大于 `BaselineDistanceThreshold`。
4. 如果当前已有持仓，则跳过所有新信号。
5. **做多条件**：Tenkan > Kijun，Kijun > Senkou Span A，Senkou Span A > Senkou Span B（云层上升），且收盘价 > Kijun。
6. **做空条件**：上述条件完全反向（Tenkan < Kijun < Senkou Span A < Senkou Span B，且收盘价 < Kijun）。
7. 平仓依赖 `StartProtection` 设置的止损/止盈，不额外触发手动退出，与原策略保持一致。

## 风险控制
策略启动时调用 `StartProtection`。若止损或止盈参数为 0，则对应保护不会启用。下单使用 `BuyMarket`/`SellMarket` 市价函数并附带预设的 SL/TP。

## 实现细节
- 多空都使用 +DI 作为动量过滤，复现了原始 MQL5 代码（原作者注释掉了 -DI 分支）。
- 未单独读取 Chikou Span，通过 Senkou Span A、B 的相对位置来验证云层方向。
- 通过内部字段保存 +DI 最新两次数值，避免调用 `GetValue`，符合高层 API 规范。
- 当两个时间框架相同时时，Ichimoku 与 ADX 共用同一个订阅，减少资源消耗。

## 使用建议
- 若希望忠实复刻 MT5 表现，请保持 ADX 时间框架更快（如 M1）而 Ichimoku 较慢（如 H1）。
- 在波动性较大的品种上可提高 `BaselineDistanceThreshold`，以要求更强的 Tenkan/Kijun 分离度。
- 策略一次仅持有一笔仓位，建议结合账户或组合层面的风险管理工具。
