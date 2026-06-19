# Super Woodies CCI 策略

该策略是原始 MQL5 **Exp_SuperWoodiesCCI** 专家的移植版本。它基于较高周期上的商品通道指数（CCI）方向进行交易。

## 逻辑

- 计算具有可配置周期的 CCI。
- 当 CCI 上穿零轴时：
  - 可选择平掉空头头寸。
  - 可选择开立多头头寸。
- 当 CCI 下穿零轴时：
  - 可选择平掉多头头寸。
  - 可选择开立空头头寸。

策略只处理已完成的K线，并在指定的 K 线类型上运行。

## 参数

- **CciPeriod** – CCI 的计算周期。
- **CandleType** – 要分析的 K 线时间框架。
- **AllowLongEntry** – 是否允许开多。
- **AllowShortEntry** – 是否允许开空。
- **AllowLongExit** – 当 CCI 为负时是否允许平多。
- **AllowShortExit** – 当 CCI 为正时是否允许平空。

## 说明

该策略使用 StockSharp 高级 API，通过 `SubscribeCandles` 订阅数据并绑定指标，交易使用 `BuyMarket` 和 `SellMarket` 方法管理头寸。
