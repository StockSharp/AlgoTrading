# 颜色零延迟 DeMarker 策略

该策略将 MQL5 专家顾问 `Exp_ColorZerolagDeMarker` 转换为 StockSharp 框架。策略使用多个 **DeMarker** 指标的加权组合构建快线和慢线，当两条线交叉时产生交易信号。

## 指标

- 使用五个不同周期的 DeMarker 指标：8、21、34、55 和 89。
- 每个指标乘以相应的权重因子：0.05、0.10、0.16、0.26、0.43。
- 加权结果求和形成 **快线**。
- **慢线** 为快线的指数平滑版本，由 `Smoothing` 参数控制。

## 交易逻辑

1. 按设定的周期订阅蜡烛数据。
2. 在每根完成的蜡烛上计算快线和慢线。
3. 若上一根快线在慢线上方而当前快线跌破慢线：
   - 若允许，平掉空头仓位。
   - 若开启，建立多头仓位。
4. 若上一根快线在慢线下方而当前快线突破慢线：
   - 若允许，平掉多头仓位。
   - 若开启，建立空头仓位。
5. 新开仓位可选用百分比止损和止盈。

## 参数

- `CandleTimeframe` – 蜡烛周期。
- `Smoothing` – 慢线平滑系数。
- `Factor1`–`Factor5` – 各 DeMarker 周期的权重。
- `DeMarkerPeriod1`–`DeMarkerPeriod5` – DeMarker 指标周期。
- `Volume` – 下单数量。
- `OpenBuy` / `OpenSell` – 是否允许开多/开空。
- `CloseBuy` / `CloseSell` – 是否允许平多/平空。
- `StopLossPct` / `TakeProfitPct` – 百分比止损与止盈。

## 说明

策略仅在蜡烛收盘后操作，并使用高层 StockSharp API（`SubscribeCandles` 和 `Bind`）。代码注释全部为英文，便于跨语言阅读。
