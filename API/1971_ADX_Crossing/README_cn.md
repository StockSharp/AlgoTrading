# ADX 交叉策略

**ADX 交叉策略**基于平均趋向指数 (ADX) 指标，通过分析正向指标线 (+DI) 与负向指标线 (-DI) 的交叉来识别趋势变化。

当 +DI 上穿 -DI 时视为看涨信号，策略可以开多仓并可选择平掉现有空仓。相反，当 +DI 下穿 -DI 时视为看跌信号，策略开空并可选择平掉多仓。策略通过内置的风险管理支持可选的止损和止盈。

## 指标

该策略使用 StockSharp 的 `AverageDirectionalIndex` 指标。策略仅使用方向线，ADX 主线不用于决策。

## 参数

- `ADX Period` – ADX 计算周期，默认 `50`。
- `Candle Type` – 使用的K线周期，默认 `1 小时`。
- `Allow Buy Open` – 是否允许开多，默认 `true`。
- `Allow Sell Open` – 是否允许开空，默认 `true`。
- `Allow Buy Close` – 是否允许在卖出信号时平多，默认 `true`。
- `Allow Sell Close` – 是否允许在买入信号时平空，默认 `true`。
- `Stop Loss` – 以绝对价格表示的止损距离，默认 `1000`。
- `Take Profit` – 以绝对价格表示的止盈距离，默认 `2000`。

## 交易逻辑

1. 订阅指定周期的K线并计算 ADX 指标。
2. 跟踪 +DI 和 -DI 的前一值以检测交叉。
3. 当 +DI 上穿 -DI：
   - 若启用 `Allow Sell Close`，平掉空头。
   - 若启用 `Allow Buy Open`，开立多头。
4. 当 +DI 下穿 -DI：
   - 若启用 `Allow Buy Close`，平掉多头。
   - 若启用 `Allow Sell Open`，开立空头。
5. 使用 `StartProtection` 应用止损和止盈。

## 注意事项

- 仅处理已完成的K线 (`CandleStates.Finished`)。
- 策略依赖 StockSharp 内置的风险管理来执行止损/止盈。
- 平仓通过发送相反方向的市价单完成。

该策略仅用于教育目的，在实盘交易前可能需要进一步优化。
