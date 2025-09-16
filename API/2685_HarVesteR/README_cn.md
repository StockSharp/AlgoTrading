# HarVesteR 策略
[English](README.md) | [Русский](README_ru.md)

HarVesteR 策略将 MACD 动能、两条简单移动平均线以及可选的 ADX 趋势强度过滤器结合起来。
当价格贴近均线运行并且 MACD 最近穿越零轴时，系统寻找突破盘整的机会。
止损挂在近期高低点，达到设定的盈亏比后先平掉一半仓位，剩余仓位依靠快速均线控制的保本退出。

## 细节

- **入场条件**：
  - 多头：`MACD > 0 && MACD 历史包含负值 && Close < SlowSMA && Close + Indentation > FastSMA && Close + Indentation > SlowSMA && ADX ≥ AdxBuyLevel (启用时)`
  - 空头：`MACD < 0 && MACD 历史包含正值 && Close > SlowSMA && Close - Indentation < FastSMA && Close - Indentation < SlowSMA && ADX ≥ AdxSellLevel (启用时)`
- **止损**：最近 `StopLookback` 根已收盘 K 线的最高价/最低价。
- **分批止盈**：价格相对入场价突破 `HalfCloseRatio × (入场价-止损价)` 后，平掉一半头寸，并把止损移动到入场价。
- **最终退出**：
  - 多头：止损已经移动到保本后，若价格跌破 `FastSMA + Indentation` 则清仓。
  - 空头：止损已经移动到保本后，若价格升破 `FastSMA + Indentation` 则清仓。
- **多空方向**：支持双向交易。
- **过滤器**：可选的 ADX 趋势过滤器，`UseAdxFilter = false` 时不做强度检查。
- **仓位管理**：新的反向信号会下单 `Volume + |Position|`，从而直接反手而无需手动平仓。

## 参数

| 名称 | 默认值 | 说明 |
|------|--------|------|
| `MacdFast` | 12 | MACD 差值线的快速 EMA 周期。 |
| `MacdSlow` | 24 | MACD 差值线的慢速 EMA 周期。 |
| `MacdSignal` | 9 | MACD 信号线的 EMA 周期。 |
| `MacdLookback` | 6 | 检查 MACD 符号变化的最近 K 线数量。 |
| `SmaFastLength` | 50 | 快速简单移动平均线长度。 |
| `SmaSlowLength` | 100 | 慢速简单移动平均线长度。 |
| `MinIndentation` | 10 | 进入与退出时围绕均线的点数偏移。 |
| `StopLookback` | 6 | 计算初始止损所用的回看区间。 |
| `UseAdxFilter` | false | 是否启用 ADX 趋势过滤器。 |
| `AdxBuyLevel` | 50 | 启用过滤器时，多头信号允许的最小 ADX。 |
| `AdxSellLevel` | 50 | 启用过滤器时，空头信号允许的最小 ADX。 |
| `AdxPeriod` | 14 | ADX 指标的计算周期。 |
| `HalfCloseRatio` | 2 | 分批止盈所需的距离倍数。 |
| `Volume` | 1 | 新开仓单的基础手数（会与当前仓位净额合并）。 |
| `CandleType` | 1 小时 | 生成蜡烛图和指标的主时间框架。 |

## 说明

- `MinIndentation` 会按照标的价格最小变动转换成实际价差；对于 3 或 5 位小数的报价，会额外乘以 10 以近似“点”这一单位。
- 当 `UseAdxFilter` 为 `false` 时，策略不会检查 ADX 值即可触发信号。
- 分批止盈与保本逻辑在每根收盘 K 线上都会执行，以便在暂停开仓时仍能管理已有头寸。
