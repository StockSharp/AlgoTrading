# 平滑均线突破策略

## 概述
平滑均线突破策略再现了原始 MQL5 专家顾问 **Smoothing Average (barabashkakvn's edition)** 的逻辑。策略将可配置的移动平均线与按点数衡量的距离过滤器结合使用。当价格偏离均线达到设定的点数时，系统会顺势开仓（若启用反向模式，则反向开仓）。当价格穿越扩大后的均线通道时，仓位被平仓。

## 交易逻辑
### 标准模式（`ReverseSignals = false`）
- **做多开仓：** 收盘价高于 `MA - Entry Delta (pips)`。
- **做空开仓：** 收盘价低于 `MA + Entry Delta (pips)`。
- **做空平仓：** 收盘价高于 `MA + Entry Delta (pips) × Close Delta Coefficient`。
- **做多平仓：** 收盘价低于 `MA - Entry Delta (pips) × Close Delta Coefficient`。

### 反向模式（`ReverseSignals = true`）
- **做多开仓：** 收盘价低于 `MA + Entry Delta (pips)`。
- **做空开仓：** 收盘价高于 `MA - Entry Delta (pips)`。
- **做多平仓：** 收盘价低于 `MA - Entry Delta (pips) × Close Delta Coefficient`。
- **做空平仓：** 收盘价高于 `MA + Entry Delta (pips) × Close Delta Coefficient`。

移动平均线可以向前平移若干根 K 线。策略通过保存最近的指标值并取 `MaShift` 根之前的数值来模拟这一效果，与 MetaTrader 中指标绘制的平移线一致。

## 参数
- `Candle Type` – 参与计算的 K 线类型。
- `MA Length` – 平滑均线的周期长度。
- `MA Shift` – 均线向前平移的 K 线数量。
- `MA Type` – 均线类型（简单、指数、平滑、线性加权）。
- `Price Source` – 输入到均线中的价格（默认使用典型价）。
- `Entry Delta (pips)` – 触发开仓所需的点数距离，按合约的最小变动价位转换为价格。
- `Close Delta Coefficient` – 计算平仓通道时对入场点数的倍数。
- `Reverse Signals` – 是否反转多空条件。
- `Trade Volume` – 每次下单的固定手数。

## 风险管理
- 所有订单均采用 `Trade Volume` 指定的固定手数，不在持仓期间加仓或减仓。
- 平仓完全依赖规则，不会主动提交止损或止盈，但会调用 `StartProtection()` 以启用平台的安全保护。
- 反向模式允许在不修改其他参数的情况下进行逆势交易。

## 实现细节
- 点值来自 `Security.PriceStep`。对于三位或五位报价的外汇品种，点值会按 MQL5 版本的方式乘以 10。
- 均线使用 `Price Source` 参数，可匹配原始 EA 中对不同价格的选择。
- 条件判断使用 K 线收盘价，作为原始程序中 bid/ask 检查的稳定替代。
- C# 源码中的注释全部采用英文，以符合转换指引的要求。
