# Executor AO 策略

## 概述
Executor AO 策略来源于 MetaTrader 平台上的 “Executor AO” 专家顾问。StockSharp 版本保留了基于 Awesome Oscillator
（AO）拐点的交易逻辑，并以固定下单手数的方式重写资金管理。策略订阅 `CandleType` 参数指定的周期，监控最新
三个收盘柱的 AO 值，只要 AO 在零轴下方形成向上的“碟形”或在零轴上方形成向下的“碟形”，便开启对应方向的
净头寸。可选的止损、止盈与移动止损规则完全按照原 EA 的参数进行转换。

## 交易逻辑
1. 订阅指定周期的 K 线，并将收盘柱输入到 AO 指标。AO 的快慢周期由 `AoShortPeriod` 与 `AoLongPeriod` 控制。
2. 保存最近三个完成柱的 AO 数值，以模拟 MetaTrader 指标缓冲区的读取方式。
3. 当没有持仓时：
   - **做多条件**：最新 AO 值大于上一柱，上一柱又低于再前一柱（形成谷底），同时最新 AO 小于
     `-MinimumAoIndent`。满足条件即按照 `TradeVolume` 下单买入。
   - **做空条件**：最新 AO 值小于上一柱，上一柱又高于再前一柱（形成峰值），并且最新 AO 大于
     `MinimumAoIndent`。满足条件即按照固定手数卖出。
4. 当存在持仓时，按照以下规则离场：
   - 根据入场价和 `StopLossPips`、`TakeProfitPips` 计算止损与止盈价位。`CalculatePipSize()` 会根据报价精度（含
     3 位或 5 位小数）自动换算点值，复制原 EA 的行为。
   - 当浮盈超过 `TrailingStopPips + TrailingStepPips` 时启动移动止损，只要新的止损位置距离价格不小于
     `TrailingStepPips` 设定，就把止损沿趋势方向推进。
   - 多头在触发止盈、止损或上一柱 AO 值转为正值时平仓；空头在触发止盈、止损或上一柱 AO 值转为负值时平仓。
5. 所有委托均为市价单，StockSharp 的净头寸模型确保同一时间只持有单一方向的仓位。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 5 分钟 | 用于生成信号的主时间框架。 |
| `TradeVolume` | `decimal` | `1` | 每次入场使用的固定手数。 |
| `AoShortPeriod` | `int` | `5` | AO 快速简单移动平均的长度。 |
| `AoLongPeriod` | `int` | `34` | AO 慢速简单移动平均的长度。 |
| `MinimumAoIndent` | `decimal` | `0.001` | 信号触发前 AO 必须与零轴保持的最小距离。 |
| `StopLossPips` | `decimal` | `50` | 以点数表示的止损距离，设置为 `0` 可关闭止损。 |
| `TakeProfitPips` | `decimal` | `50` | 以点数表示的止盈距离，设置为 `0` 可关闭止盈。 |
| `TrailingStopPips` | `decimal` | `5` | 移动止损的基础距离，大于 0 时启用。 |
| `TrailingStepPips` | `decimal` | `5` | 移动止损每次推进所需的最小点数，启用移动止损时必须保持正值。 |

## 与原版 EA 的差异
- MetaTrader 版本支持按账户风险百分比计算手数，移植版本仅提供固定手数（`TradeVolume`），以便在 Designer 和
  API 中直观配置。
- 止损和止盈在策略内部监控：当价格在收盘柱内触及目标时，策略发送市价反向单平仓，而不是注册独立的挂单。
- 移动止损在每根收盘柱结束时检查，这符合 StockSharp 高级 API 的工作方式，同时仍然按照原 EA 的阈值计算。
- 指标处理完全依赖 `SubscribeCandles` 与 `Bind` 的高阶接口，不再手动复制指标缓冲区。

## 使用建议
- 在启动策略前，将 `TradeVolume` 调整为交易品种允许的最小步长倍数，并注意 `Strategy.Volume` 会同步到相同数值。
- 如果 AO 在零轴附近频繁震荡，可提高 `MinimumAoIndent` 过滤噪音；设为 `0` 则复制原 EA 的激进模式。
- 启用移动止损时务必保持 `TrailingStepPips` 大于零，否则会抛出异常，以提示参数设置有误。
- 建议在图表上同时绘制 AO 指标和策略成交，以便验证转换后的拐点识别是否符合预期。

## 指标
- **Awesome Oscillator**：使用中位价的 5/34 简单移动平均差值，完全对应 MetaTrader 标准指标。
