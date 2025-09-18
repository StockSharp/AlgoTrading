# Follow Line 策略

## 概述
Follow Line 策略完整移植自 MetaTrader 顾问 `FollowLineEA_v1.0`。策略通过布林带突破来更新一条“跟随线”，这条线在多头趋势中只会上移，在空头趋势中只会下移。为了避免过早触发，还可以用 ATR 做缓冲。额外的简单移动平均线用于确认信号，行为与原始 EA 一致。

## 交易逻辑
1. **指标链**
   - 布林带 (`BollingerPeriod`, `BollingerDeviations`) 检测突破。
   - ATR (`AtrPeriod`) 在 `UseAtrFilter` 为真时扩展跟随线。
   - 一组 SMA (`MovingAveragePeriod`) 计算高、低、开、收、媒价的平均值，以便在 `OpenCloseMedian` 或 `HighLowOpenClose` 模式下过滤信号。
2. **跟随线更新**
   - 收盘价高于上轨时，线被抬到最低价（可减去 ATR）；如果新值低于之前的线，就保持旧值。
   - 收盘价低于下轨时，线被压到最高价（可加上 ATR）；如果新值高于旧线，也保持旧值。
   - 跟随线的增量方向决定趋势状态。
3. **信号生成**
   - 趋势从空翻多时触发买入箭头；多翻空时触发卖出箭头。
   - `IndicatorsShift` 指定了信号与执行之间的延迟（以 K 线数量计），完全对齐 MQL `iCustom` 的 shift 参数。
4. **过滤条件**
   - `UseTimeFilter`、`TimeStartTrade`、`TimeEndTrade` 控制交易时段，可跨越午夜。
   - `MaxSpread` 限制点差，`MaxOrders` 限制仓位。

## 风险控制
- `CloseInSignal` 遇到反向箭头立即平仓。
- `CloseInProfit` 与 `CloseInLoss` 根据 `PipsCloseProfit`/`PipsCloseLoss` 在达到指定点数时关闭仓位，`UseBasketClose` 模拟原版的“篮子”逻辑。
- `UseStopLoss`、`UseTakeProfit`、`UseTrailingStop`、`UseBreakEven` 组成完整的保护系统，参数单位为价格步长。
- `AutoLotSize` 与 `RiskFactor` 根据资金自动计算手数，否则使用 `ManualLotSize`，下单前会按交易所最小步长和最小/最大手数做归一化。

## 参数
所有原始参数均已实现，包括：
- 通用：`CandleType`、`BarsCount`。
- 指标：`BollingerPeriod`、`BollingerDeviations`、`MovingAveragePeriod`、`AtrPeriod`、`UseAtrFilter`、`TypeOfArrows`、`IndicatorsShift`。
- 过滤器：`UseTimeFilter`、`TimeStartTrade`、`TimeEndTrade`、`MaxSpread`、`MaxOrders`。
- 风险：`CloseInSignal`、`UseBasketClose`、`CloseInProfit`、`PipsCloseProfit`、`CloseInLoss`、`PipsCloseLoss`、`UseTakeProfit`、`TakeProfit`、`UseStopLoss`、`StopLoss`、`UseTrailingStop`、`TrailingStop`、`TrailingStep`、`UseBreakEven`、`BreakEven`、`BreakEvenAfter`。
- 资金管理：`AutoLotSize`、`RiskFactor`、`ManualLotSize`。

## 使用提示
- 策略仅处理收盘完结的 K 线，适合与历史测试同步使用。
- `TypeOfArrows = HideArrows` 可以暂停交易而保留指标绘制。
- 默认会创建图表区域并显示交易。

## 转换细节
- 完全使用 StockSharp 高级 API，无需手工维护指标缓冲区。
- 通过 `BuyMarket`/`SellMarket` 和保护单工具方法实现下单与风控。
- 代码注释采用英文，符合仓库规范。
