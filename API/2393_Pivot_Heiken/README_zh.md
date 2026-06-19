# Pivot Heiken 策略

该策略结合日内枢轴点与 Heikin-Ashi 蜡烛，并可选用追踪止损。日枢轴点由前一日的最高、最低和收盘价计算。Heikin-Ashi 平滑价格噪音，突出趋势方向。

## 策略逻辑
- **做多**：Heikin-Ashi 蜡烛为阳线且收盘价高于日枢轴。
- **做空**：Heikin-Ashi 蜡烛为阴线且收盘价低于日枢轴。
- **平仓**：达到止损、止盈或触发追踪止损。

## 参数
- `CandleType` – 工作蜡烛类型。
- `StopLossPips` – 止损点数。
- `TakeProfitPips` – 止盈点数。
- `TrailingStopPips` – 追踪止损点数（0 表示关闭）。

## 指标
- Heikin-Ashi（在策略内部计算）。
- 日枢轴点。

## 说明
- 使用高级 API，基于蜡烛订阅和指标绑定。
- 适用于多空交易。
