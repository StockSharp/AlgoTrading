# MACD EA 策略

该策略基于 `MQL/20010` 目录中的 MetaTrader 5 专家顾问 `MACD EA (barabashkakvn's edition).mq5`，移植到 StockSharp 平台。转换后的版本完整保留了原始 EA 的 MACD 信号、分批止盈以及资金管理逻辑，并采用 StockSharp 的高级 API 实现。

## 交易思路

* **信号来源**：使用可配置的快速、慢速、信号周期计算 MACD 指标。策略比较两根及四根已完成 K 线的 MACD 主线与信号线差值，当差值由负转正时做多，反之做空。
* **持仓管理**：入场时设置以点数计的止损和止盈距离。距离会根据标的最小报价步长换算成实际价格；若品种保留 3 或 5 位小数，则将步长额外乘以 10，与原始 EA 的点值调整保持一致。
* **分批止盈**：当浮盈达到 `PartialProfitPips` 点时，平掉一半仓位，剩余仓位继续持有。
* **保本逻辑**：价格向有利方向移动 `BreakevenPips` 点后，启动保本保护。如果价格回落到开仓价，则立即在开仓价平仓，相当于 EA 将止损移动到成本价。
* **反向信号退出**：一旦出现反向的 MACD 交叉，会平掉剩余仓位，避免与指标方向相反的持仓。

## 资金管理

启用 `UseMoneyManagement` 后，若连续出现亏损，下一笔交易会增加手数。倍率取决于连续亏损次数（一次亏损后乘以 2，两次亏损后乘以 3，依此类推，最多乘以 7）。最终的下单量等于该倍率与 `RiskMultiplier` 参数的乘积，模拟原策略的类马丁操作。盈利交易会将亏损计数重置为零。

## 参数

| 参数 | 说明 |
|------|------|
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | MACD 指标的周期设置。
| `StopLossPips` | 止损距离（点），0 表示不设置。
| `TakeProfitPips` | 止盈距离（点），0 表示不设置。
| `PartialProfitPips` | 触发分批止盈所需的点数，0 表示不启用。
| `BreakevenPips` | 启动保本所需的点数，0 表示不启用。
| `UseMoneyManagement` | 是否开启基于连亏的仓位放大。
| `RiskMultiplier` | 资金管理激活时使用的额外倍率。
| `BaseVolume` | 放大前的基础下单量。
| `CandleType` | 用于计算指标的 K 线类型。

## 说明

* 策略通过 `SubscribeCandles` 订阅 K 线并绑定指标，遵循推荐的高级 API 用法。
* 目前仅提供 `CS` 目录下的 C# 实现，暂未提供 Python 版本。
* 按要求未修改或添加任何测试用例。
