# 基于RSI自动交易策略

该策略对最近的RSI值进行平均以生成交易信号。它首先按照可配置的`RsiPeriod`计算相对强弱指数（RSI），随后对RSI本身应用简单移动平均线。平均后的RSI超过或跌破设定阈值时开仓，并在出现相反信号时平仓。

## 交易逻辑

1. **RSI计算**
   - 使用`RsiPeriod`参数根据蜡烛的收盘价计算RSI。
2. **RSI平均**
   - 通过简单移动平均对最近`AveragePeriod`个RSI值进行平滑。
3. **入场规则**
   - 当`BuyEnabled`为`true`且当前无持仓时，若平均RSI高于`BuyThreshold`（默认55），则开多单。
   - 当`SellEnabled`为`true`且当前无持仓时，若平均RSI低于`SellThreshold`（默认45），则开空单。
4. **出场规则**
   - 当`CloseBySignal`为`true`时，根据相反信号平仓：
     - 多头持仓在平均RSI跌破`CloseBuyThreshold`（默认47）时平仓。
     - 空头持仓在平均RSI升破`CloseSellThreshold`（默认52）时平仓。

## 参数

- `BuyEnabled` – 是否允许做多。
- `SellEnabled` – 是否允许做空。
- `CloseBySignal` – 是否在相反RSI信号出现时平仓。
- `RsiPeriod` – RSI计算周期。
- `AveragePeriod` – 用于平均的RSI数量。
- `BuyThreshold` – 平均RSI高于该值时开多。
- `SellThreshold` – 平均RSI低于该值时开空。
- `CloseBuyThreshold` – 平均RSI低于该值时平多。
- `CloseSellThreshold` – 平均RSI高于该值时平空。
- `CandleType` – 订阅的蜡烛类型。

## 注意

本策略展示了如何在StockSharp高级API中通过绑定组合多个指标。为了简化，原MQL版本中的跟踪止损和资金管理功能未被实现。

