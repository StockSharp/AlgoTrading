# Exp Oracle 策略

该策略是 MetaTrader **Exp_Oracle** 专家的 C# 移植版。它依赖自定义的 *Oracle* 指标，该指标将相对强弱指数 (RSI) 与商品通道指数 (CCI) 结合，用于预测未来数根K线的走势。指标输出两条线：

- **Oracle 线**：CCI 与 RSI 极值的组合。
- **Signal 线**：Oracle 线的平滑移动平均。

策略提供三种信号模式来解释这些线：

1. **Breakdown** – 当 Signal 线穿越零轴时开仓。
2. **Twist** – 当 Signal 线出现转折点时开仓。
3. **Disposition** – 当 Signal 线与 Oracle 线发生交叉时开仓。

## 参数

- `OraclePeriod` – 计算 RSI 与 CCI 的周期。
- `Smooth` – 平滑 Signal 线所使用的周期数。
- `Mode` – 生成交易信号的算法（`Breakdown`、`Twist` 或 `Disposition`）。
- `CandleType` – 使用的K线周期。
- `AllowBuy` – 是否允许做多。
- `AllowSell` – 是否允许做空。
- `Volume` – 交易量，继承自 `Strategy` 基类。

## 入场与出场规则

### Breakdown
- Signal 线向上穿越零轴时买入。
- Signal 线向下穿越零轴时卖出。

### Twist
- Signal 线下降后转向上升时买入。
- Signal 线上升后转向下降时卖出。

### Disposition
- Signal 线向上穿越 Oracle 线时买入。
- Signal 线向下穿越 Oracle 线时卖出。

出现反向信号时，持仓会被平仓并反向建立新仓。策略使用市价单实现。

## 指标逻辑

每根K线的计算步骤：
1. 使用 `OraclePeriod` 计算 RSI 与 CCI。
2. 根据最近的 CCI 与 RSI 值构建四个差值。
3. Oracle 线等于这四个差值中最大值与最小值之和。
4. Signal 线是 Oracle 线在 `Smooth` 根K线上的简单移动平均。

该方法通过结合动量 (RSI) 与通道 (CCI) 信息来尝试预测短期价格走势。

## 说明

- 策略仅在K线收盘后运行。
- 未实现保护性止损，请根据需要自行管理风险。

