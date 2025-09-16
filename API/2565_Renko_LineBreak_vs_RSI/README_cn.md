# Renko Line Break vs RSI 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 MetaTrader 的 “RenkoLineBreak vs RSI” 专家顾问，使用 StockSharp 的高级 API 重新实现。策略利用 Renko 砖块判断趋势方向，同时通过 RSI 回调过滤信号，并围绕三根 K 线结构挂出止损挂单。

## 细节

- **入场条件**：
  - **做多**：Renko 趋势保持上行，且 RSI 下降到 `50 - RsiShift` 或更低。买入止损单挂在三根柱之前的最高价上方加上 `IndentFromHighLow`。
  - **做空**：Renko 趋势保持下行，且 RSI 上升到 `50 + RsiShift` 或更高。卖出止损单挂在三根柱之前的最低价下方减去 `IndentFromHighLow`。
  - 当 Renko 出现过渡状态（`ToUp` / `ToDown`）时，会自动撤销挂单。
- **方向**：多空双向。
- **出场条件**：
  - 出现相反的 Renko 过渡信号（做多遇到 `ToDown`，做空遇到 `ToUp`）。
  - RSI 回到中线附近（`50 ± RsiShift`）。
  - K 线触及预设的止损或止盈价格。
- **止损/止盈**：
  - 止损放在最近三根 K 线的极值，并加上 `IndentFromHighLow` 缓冲。
  - 止盈距离计划入场价 `TakeProfit` 个价格单位（设置为 0 可关闭）。
- **默认参数**：
  - `BoxSize` = 500m。
  - `RsiPeriod` = 4。
  - `RsiShift` = 20m。
  - `TakeProfit` = 1000m。
  - `IndentFromHighLow` = 50m。
  - `Volume` = 1m。
  - `CandleType` = 5 分钟周期。
- **筛选标签**：
  - 类型：趋势跟随。
  - 方向：多空皆可。
  - 指标：Renko、RSI。
  - 止损：固定止损和止盈。
  - 复杂度：中等。
  - 周期：Renko + 时间结合。
  - 季节性：无。
  - 神经网络：无。
  - 背离：无。
  - 风险等级：中等。

## 工作流程

1. 订阅 `RenkoCandleMessage`，通过 Renko 砖块判断方向。砖块翻转时，趋势状态会暂时变为 `ToUp` 或 `ToDown`，模拟原指标的信号。
2. 同时订阅时间 K 线，用于计算 RSI，并提供最近三根柱体的高低点以确定突破价位。
3. 当 Renko 趋势和 RSI 条件同时满足时，策略注册相应方向的止损挂单，并保存计划中的止损/止盈价格。
4. 挂单成交后，保存的保护价位开始生效。每根后续 K 线都会检查是否触碰止损或止盈，若触及则以市价平仓。
5. 若 RSI 再次穿越中线或 Renko 指示趋势扭转，仓位会提前平掉。

## 使用的指标

- **Renko 砖块**：识别趋势方向及其过渡阶段。
- **RSI 指标**：要求顺势中的回调，从而过滤入场信号。

## 补充说明

- `IndentFromHighLow` 重现了原策略中的缓冲距离，避免订单贴近最近的高低点。
- 将 `TakeProfit` 设为 0 时，不再设置止盈，止损逻辑仍然保留。
- 策略同一时间只保留一张挂单，当条件失效会自动撤单。
