# EMA Cross Contest Hedged 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 中重现 MetaTrader 智能交易系统 **EMA_CROSS_CONTEST_HEDGED**。机器人监控快慢指数移动平均线（EMA）的交叉，并可选使用 MACD 直方图过滤趋势。当出现信号时，策略立即以市价开仓，并放置一组分批的止损挂单，当价格继续沿趋势运行时逐步加仓以对冲风险。

## 交易逻辑
- 在指定的K线数据上计算快 EMA 和慢 EMA。信号可以基于上一根完成的K线（默认）或当前K线收盘后生成。
- 当快 EMA 上穿慢 EMA 时认为出现**多头交叉**，下穿时认为出现**空头交叉**。
- 若启用 MACD 过滤，则做多需要 MACD 线上方为正，做空需要 MACD 线为负。
- 多头信号触发时，以市价买入，设置止损和止盈，并在价格上方按固定间距排列四个买入止损挂单。
- 空头信号触发时，以市价卖出，并在价格下方设置四个卖出止损挂单。
- 如果挂单在到期时间之前未被触发，将自动取消。
- 浮动盈利扩大时启动追踪止损；若参数 `Use Close` 启用，则相反方向的交叉会提前平仓。

## 参数
- **Candle Type** – 所用K线类型/周期。
- **Order Volume** – 初始头寸及每个对冲挂单的交易量。
- **Take Profit (pips)** – 止盈距离（点）。
- **Stop Loss (pips)** – 止损距离（点）。
- **Trailing Stop (pips)** – 追踪止损距离（点，0 表示关闭）。
- **Hedge Level (pips)** – 对冲挂单之间的间隔（点）。
- **Use Close** – 当出现反向交叉时是否平仓。
- **Use MACD** – 是否需要 MACD 确认。
- **Expiration (s)** – 挂单有效时间（秒）。
- **Short EMA** – 快速 EMA 的周期。
- **Long EMA** – 慢速 EMA 的周期（必须大于快速 EMA）。
- **Signal Bar** – 取信号的K线：0=当前K线，1=上一根K线。

## 说明
- 按照要求，代码中的注释全部使用英文。
- 对冲挂单的排列方式与原始 MQL 策略一致，共包含四个等距级别。
- 点数转换为价格时会考虑交易品种的 `PriceStep` 与 `Decimals`，以贴近 MetaTrader 的计算方式。
