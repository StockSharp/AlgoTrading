# 3874 Trendcapture 策略

## 概述

**Trendcapture 策略** 是 MetaTrader 专家顾问 `MQL/7772/Trendcapture.mq4` 的 StockSharp 高层 API 迁移版本。原始脚本通过 Parabolic SAR 判断趋势方向，并且只在 ADX 显示弱趋势时进场。每次平仓后，它会根据已实现的盈亏决定下一笔交易的方向，同时在持仓获利达到若干点时，把止损上移到保本价。

本移植版本保留了这些规则，使用 StockSharp 的指标绑定与订单辅助方法，只在选定周期的收盘 K 线上做出决策。

## 交易逻辑

1. **指标**
   - Parabolic SAR（`ParabolicSar`），步长与最大加速度均可配置。
   - 平均趋向指数（`AverageDirectionalIndex`），用于取得主线数值。
2. **开仓条件**
   - 任意时刻最多持有一张仓位。
   - 多头信号要求：
     - 上一次平仓后得到的“期望方向”为买入。
     - 当前 K 线收盘价高于 SAR 值。
     - ADX 主线低于 `20`，对应原策略中“趋势疲弱”的判定。
   - 空头条件完全镜像：期望方向为卖出、收盘价低于 SAR、ADX 低于 `20`。
3. **持仓管理**
   - 每次成交后立即下达止损与止盈单，距离分别为 `StopLossPoints` 与 `TakeProfitPoints`（会根据品种的价格步长换算成绝对价格）。
   - 当浮盈达到 `GuardPoints` 设定的阈值时，撤销原止损并在入场价重新挂出，以复刻原脚本的保本逻辑。
   - 平仓后，如果本次交易盈利则保持原方向，若亏损或打平则反向——对应 MQL 代码里的 `OrderProfit()` 判断。

## 参数

| 参数 | 含义 | 默认值 |
|------|------|--------|
| `CandleType` | 进行信号运算的 K 线类型。 | 1 小时 K 线 |
| `SarStep` | Parabolic SAR 的初始加速度。 | `0.02` |
| `SarMax` | Parabolic SAR 的最大加速度。 | `0.2` |
| `AdxPeriod` | ADX 的平滑周期。 | `14` |
| `TakeProfitPoints` | 止盈距离（价格步数）。 | `180` |
| `StopLossPoints` | 止损距离（价格步数）。 | `50` |
| `GuardPoints` | 触发保本的浮盈距离（价格步数）。 | `5` |
| `MaximumRisk` | 成交量缩放因子；`0.03` 与原脚本的头寸大小一致。 | `0.03` |

## 使用说明

- 请确认所选品种提供 `PriceStep`（或至少 `MinStep`），策略才能把点数正确转换成价格。
- `Volume` 属性表示在 `MaximumRisk = 0.03` 时的基础手数，提高风险系数会按比例放大下单数量。
- 策略只发送市价单，并立即挂出保护性止损/止盈，因此空仓状态下账簿里不会遗留挂单。
- 保本功能通过撤单并在入场价重新挂出止损来实现，对应 MQL 代码中 `OrderModify` 把止损移动到开仓价的行为。

## 文件列表

- `CS/TrendcaptureStrategy.cs` —— Trendcapture 策略的 StockSharp 实现。
- `README.md` —— 英文说明。
- `README_ru.md` —— 俄文说明。
