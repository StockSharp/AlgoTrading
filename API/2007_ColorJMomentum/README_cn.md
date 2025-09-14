# ColorJMomentum 策略

**ColorJMomentum** 策略基于 Jurik 平滑动量指标的方向变化进行交易。该策略源自 MQL5 专家顾问 `Exp_ColorJMomentum`，并使用 StockSharp 的高级 API 重新实现。

## 思路

1. 计算所选价格序列的标准 *Momentum* 值。
2. 使用 **Jurik Moving Average (JMA)** 对动量进行平滑。
3. 监控平滑动量的最近两个数值：
   - 如果指标此前下降而现在开始上升，则开立 **多头** 头寸；
   - 如果指标此前上升而现在开始下降，则开立 **空头** 头寸。
4. 通过百分比形式的止损和止盈来保护仓位。

策略仅处理已经完成的K线，并在内部变量中保存之前的数值，无需直接访问历史指标数据。

## 参数

- **Momentum Length** – 动量计算周期。
- **JMA Length** – JMA 平滑周期。
- **Candle Type** – 订阅K线的类型和周期。
- **Stop Loss %** – 止损百分比。
- **Enable Stop Loss** – 是否启用止损。
- **Take Profit %** – 止盈百分比。
- **Enable Long** – 允许开多。
- **Enable Short** – 允许开空。

所有参数均通过 `StrategyParam` 创建，可在 Designer 中进行优化。

## 使用方法

1. 将策略连接到目标交易品种。
2. 配置参数或保留默认值（8 周期动量和 8 周期 JMA，使用 8 小时K线）。
3. 启动策略。当动量方向发生反转时，会通过 `BuyMarket` 和 `SellMarket` 发送订单。

## 说明

- 策略只处理收盘完成的K线。
- 指标颜色未显式指定，由 Designer 自动选择。
- 代码遵循项目规则，不使用 LINQ 或自定义集合。
