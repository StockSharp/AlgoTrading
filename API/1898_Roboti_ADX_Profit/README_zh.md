# Roboti ADX Profit 策略

该策略将原始的 **RobotiADXProfitwining.mq4** 专家顾问移植到 StockSharp API。核心指标为方向性移动指数（DMI），通过比较 `+DI` 和 `-DI` 线来判断趋势方向。

## 交易逻辑

- 使用 `DirectionalIndex` 指标，默认周期为 14。
- 默认在 1 小时K线上计算，可根据需要调整。
- 当 `+DI` 线向上穿越 `-DI` 且当前无多头仓位时，开多头仓位。
- 当 `-DI` 线向上穿越 `+DI` 且当前无空头仓位时，开空头仓位。
- 通过按价格百分比计算的追踪止损来保护持仓。

## 参数

| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `DmiPeriod` | DMI 计算周期。 | 14 |
| `CandleType` | 策略使用的K线类型及周期。 | 1 小时 |
| `TrailingStopPercent` | 追踪止损的百分比大小。 | 1% |

## 说明

策略使用 StockSharp 的高级绑定 API，不直接访问指标缓冲区。按要求，代码中的注释均为英文。
