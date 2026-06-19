# TDS Global 策略
[English](README.md) | [Русский](README_ru.md)

该策略复刻了 MetaTrader 平台上的 “TDSGlobal” 专家顾问，遵循 Alexander Elder 的三重筛选理念。默认使用日线
K 线，结合 MACD (12, 23, 9) 主线斜率与 24 周期的 Williams %R 指标。MACD 上升且 %R 低于阈值时寻找做多机会，
MACD 下降且 %R 高于阈值时寻找做空机会。

一旦出现信号，系统会在上一根 K 线的高点或低点之外挂入止损单，并强制与当前价格保持 `EntryBufferSteps`
个最小价位的距离，复现原始 EA 中的 “16 点” 逻辑。开仓后会跟踪保护性止损、可选的止盈以及按价位步长
移动的跟踪止损。

## 交易规则

- **数据**：默认使用日线，可通过 `CandleType` 参数调整。
- **趋势过滤**：比较最近两个 MACD 主线值。主线抬高视为多头趋势，主线下行视为空头趋势。
- **震荡过滤**：使用上一根 K 线的 Williams %R。低于 `WilliamsBuyLevel`（默认 -75）允许做多，高于
  `WilliamsSellLevel`（默认 -25）允许做空。
- **入场**：
  - 多头：在上一根高点上方一格挂入买入止损，若距离不够，则将价格提升至前收盘价上方 `EntryBufferSteps`
    个价位。
  - 空头：在上一根低点下方一格挂入卖出止损，若距离不够，则将价格降低至前收盘价下方 `EntryBufferSteps`
    个价位。
- **风险控制**：
  - 初始止损放在上一根 K 线的相反端点（多头用前低，空头用前高）。
  - 止盈距离为 `TakeProfitSteps` 个价位，默认值 999 保持与原策略类似的宽阔目标。
  - `TrailingStopSteps` 大于 0 时启用跟踪止损，只沿有利方向收紧。
- **订单管理**：
  - 当入场价或保护价位发生变化时，会取消并重新挂出对应的止损单。
  - 当 MACD 趋势反向时，撤销与当前方向不符的待挂单。
  - 开仓后复用之前计算好的价格初始化实时止损/止盈。
- **可选分时**：`UseSymbolStagger` 选项可以启用原 EA 针对 EURUSD、GBPUSD、USDCHF、USDJPY 的分钟错峰，
  避免在同一时刻提交多个挂单。

## 参数说明

- `MacdFastLength`、`MacdSlowLength`、`MacdSignalLength`：MACD 指标周期。
- `WilliamsLength`：Williams %R 的回溯长度。
- `WilliamsBuyLevel`、`WilliamsSellLevel`：多空信号阈值（负值）。
- `EntryBufferSteps`：挂单与市场之间的最小价位距离。
- `TakeProfitSteps`：止盈距离，设为较小值即可启用固定止盈。
- `TrailingStopSteps`：跟踪止损的距离，设为 0 表示关闭。
- `UseSymbolStagger`：是否启用品种错峰窗口。
- `CandleType`：计算所用的 K 线类型。

## 额外说明

- 交易量由策略的 `Volume` 属性控制，如果未设置则默认使用 1。
- 由于在收盘时处理挂单与风控，盘中成交会以存储的入场价进行近似。
- 默认的止盈距离非常大，保持与原始脚本一致；如需实际目标，请调整 `TakeProfitSteps`。
