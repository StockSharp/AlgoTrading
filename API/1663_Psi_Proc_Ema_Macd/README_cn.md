# Psi Proc EMA MACD 策略

该策略源自 MQL 专家 `e-PSI@PROC.mq4` 的 T4 系统。它根据多条指数移动平均线的排列和 MACD 过滤器进行交易。

## 策略逻辑

1. 计算 EMA(200)、EMA(50) 和 EMA(10)。
2. 使用参数 12、26、9 计算 MACD。
3. 做多条件：
   - EMA200 上升且 EMA50 > EMA200；
   - EMA50 上升且 EMA10 > EMA50；
   - MACD 上升并且大于 `LimitMACD`。
4. 做空条件：
   - EMA200 下降且 EMA50 < EMA200；
   - EMA50 下降且 EMA10 < EMA50；
   - MACD 下降并且小于 `-LimitMACD`。
5. 当收盘价跌破 EMA50 时平多。
6. 当收盘价升破 EMA50 时平空。

支持可选的止盈和移动止损保护。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `LimitMACD` | 允许进场的最小 MACD 绝对值。 |
| `TakeProfitPoints` | 以价格点表示的止盈水平。 |
| `TrailStopPoints` | 以价格点表示的移动止损。 |
| `CandleType` | 策略使用的K线周期。 |

## 说明

- 使用市价单开仓。
- 仅处理已完成的K线。
- 策略仅针对一个标的运行。

