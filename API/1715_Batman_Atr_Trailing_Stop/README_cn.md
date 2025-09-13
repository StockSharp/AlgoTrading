# Batman ATR 拖尾止损策略

该策略基于原版“Batman”专家顾问，使用 **平均真实波幅 (ATR)** 构建动态支撑和阻力线，并在价格突破这些水平时入场。

## 逻辑

1. 计算可配置周期的 ATR。
2. 根据当前价格计算支撑与阻力：
   - `support = price - ATR * factor`
   - `resistance = price + ATR * factor`
3. 根据当前趋势维护最近的支撑或阻力。
4. 价格向上突破阻力时，开多仓。
5. 价格向下突破支撑时，开空仓。

价格可选择使用收盘价或典型价 `(高+低+收)/3`。

## 参数

| 名称 | 说明 |
|------|------|
| `ATR Period` | ATR 指标的周期。 |
| `ATR Factor` | 用于生成止损线的 ATR 倍数。 |
| `Use Typical Price` | 若启用，则使用 `(高+低+收)/3`。 |
| `Candle Type` | 用于计算的 K 线类型。 |

## 备注

- 策略使用高层 API，采用 `SubscribeCandles` 与 `Bind`。
- 在启动时调用 `StartProtection()` 保护持仓。
- 仅在 K 线完成后执行交易。
