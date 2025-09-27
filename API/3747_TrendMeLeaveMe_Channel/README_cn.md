# TrendMeLeaveMe 挂单通道策略
[English](README.md) | [Русский](README_ru.md)

本策略在 StockSharp 平台上复刻了 MetaTrader 的 “TrendMeLeaveMe” 专家顾问。原版本依赖交易者在图表上手工绘制趋势线并在价格靠近时挂出突破性 stop 单。本实现使用线性回归指标自动生成通道中轴，同时按照原始 EA 的设置对上、下轨做固定点差偏移。

策略同时支持做多与做空。一旦挂单被触发，会立即根据预设距离发送止损与止盈订单，从而复制 MQL 程序里下单时附带的保护参数。挂单价格会随最新的回归值自动刷新，保持贴近当前趋势线。

## 策略流程

1. 订阅蜡烛数据并计算 `LinearRegression` 指标作为趋势中轴。
2. 用户以“价格步长”为单位配置四个偏移量（买入上/下轨与卖出上/下轨），策略将其转换成绝对价格。
3. 当最新蜡烛收盘价位于中轴与买入下偏移之间时，在上偏移位置放置 buy stop；当收盘价位于中轴与卖出上偏移之间时，在下偏移位置放置 sell stop。
4. 如果价格离开上述激活区间，则撤销对应挂单，避免在盘口中保留无效委托。
5. 挂单成交后，按照设定的点数差生成固定的止损/止盈订单。

## 交易信号

- **做多条件**：收盘价不高于回归线且仍高于买入下偏移，买入 stop 订单放在上偏移位置并随线跟踪。
- **做空条件**：收盘价不低于回归线且仍低于卖出上偏移，卖出 stop 订单放在下偏移位置并随线移动。
- **无信号**：价格位于通道外部时，撤销相应挂单。

## 风险控制

- 多头仓位使用 `BuyStopLossSteps` 与 `BuyTakeProfitSteps` 计算固定的止损和止盈距离。
- 空头仓位使用 `SellStopLossSteps` 与 `SellTakeProfitSteps` 进行同样的控制。
- 只有在净头寸方向发生变化时才重新生成保护订单，以模拟 MetaTrader 挂单自带的止损/止盈行为。

## 参数说明

- `CandleType` – 用于计算趋势线的蜡烛类型。
- `TrendLength` – 线性回归窗口长度（蜡烛数）。
- `BuyStepUpper` / `BuyStepLower` – 多头触发区间的上、下偏移（价格步长）。
- `SellStepUpper` / `SellStepLower` – 空头触发区间的上、下偏移。
- `BuyTakeProfitSteps` / `BuyStopLossSteps` – 多头仓位的止盈与止损距离。
- `SellTakeProfitSteps` / `SellStopLossSteps` – 空头仓位的止盈与止损距离。
- `BuyVolume` / `SellVolume` – 对应方向的下单数量。

## 其他说明

- 由于无法直接读取手绘趋势线，回归指标在此扮演“中轴”角色，可通过调整窗口长度来贴合主观趋势判断。
- 只有在连接正常并允许交易 (`IsFormedAndOnlineAndAllowTrading`) 时才会下单。
- 如果已有同方向仓位，策略会撤销对应的挂单，保持每个方向仅有一个待触发订单，与原 EA 的行为一致。
