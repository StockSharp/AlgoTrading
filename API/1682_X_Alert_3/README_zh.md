# X Alert 3 策略

该策略复现了 **X_alert_3.mq4** 的逻辑。它计算两条可配置参数的移动平均线，并在发生交叉时输出提示信息。

## 工作原理

1. 每根完成的K线都会计算两条移动平均线。
2. 当满足以下条件时产生看涨提示：
   - 当前K线上 MA1 高于 MA2；
   - 前一根K线上 MA1 仍高于 MA2；
   - 两根K线之前 MA1 低于 MA2。
3. 当满足以下条件时产生看跌提示：
   - 当前K线上 MA1 低于 MA2；
   - 前一根K线上 MA1 仍低于 MA2；
   - 两根K线之前 MA1 高于 MA2。
4. 策略不会开仓或平仓，只是向日志写入消息。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `Ma1Period` | 第一条移动平均线的周期。 | `1` |
| `Ma1Type` | 第一条移动平均线的类型（Simple、Exponential、Smoothed、Weighted）。 | `Simple` |
| `Ma2Period` | 第二条移动平均线的周期。 | `14` |
| `Ma2Type` | 第二条移动平均线的类型。 | `Simple` |
| `PriceType` | 计算所使用的价格（Close、Open、High、Low、Median、Typical、Weighted）。 | `Median` |
| `CandleType` | 使用的K线类型。 | 1分钟 |

## 说明

- 为了检测交叉，策略保存最近两次移动平均线差值，不直接访问历史指标数据。
- 提示信息通过 `AddInfoLog` 写入日志，不会产生其他副作用。
- 原始 MetaTrader 参数 `RunIntervalSeconds` 在 StockSharp 中无必要，因此被省略。

