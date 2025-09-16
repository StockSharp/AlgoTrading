# GO 策略

该策略是原始 MetaTrader 脚本 "GO" 的 C# 移植版本。它基于开盘价、最高价、最低价和收盘价的移动平均线计算自定义振荡器，并用其确定市场方向。

## 策略逻辑

1. 对 Open、High、Low、Close 四个价格序列使用相同周期和方法构建移动平均线。
2. 在每根完成的 K 线上计算 *GO* 值：
   
   `GO = ((MA_close - MA_open) + (MA_high - MA_open) + (MA_low - MA_open) + (MA_close - MA_low) + (MA_close - MA_high)) * Volume`
3. 当 GO 值为正时，平掉所有空头仓位并开多单。
4. 当 GO 值为负时，平掉所有多头仓位并开空单。
5. 每根 K 线上只允许一次交易，直到开仓数量达到 **Max Positions** 参数。

## 参数

- **Risk %** – 用于计算交易量的账户资金百分比。
- **Max Positions** – 同方向允许的最大持仓数量。
- **MA Type** – 移动平均线类型（SMA、EMA、DEMA、TEMA、WMA、VWMA）。
- **MA Period** – 所有移动平均线的周期。
- **Candle Type** – 用于计算的 K 线类型。

## 备注

该实现使用 StockSharp 的高级 API。策略订阅 K 线、绑定指标并在图表上绘制它们。交易量根据设置的风险百分比和品种的交易量限制自动调整。
