# Market Trend Levels Non-Repainting
[English](README.md) | [Русский](README_ru.md)

基于 EMA 交叉的策略，可选用 RSI 进行过滤。当快 EMA 上穿慢 EMA 时做多，反向交叉时做空。若启用 `ApplyExitFilters` 且开启 RSI 过滤， 当 RSI 超出设定阈值时仓位将被平掉。

## 细节

- **入场条件**：
  - **做多**：`Fast EMA` 上穿 `Slow EMA`，且开启过滤时 `RSI > RsiLongThreshold`
  - **做空**：`Fast EMA` 下穿 `Slow EMA`，且开启过滤时 `RSI < RsiShortThreshold`
- **离场条件**：反向交叉或在 `ApplyExitFilters` 下 RSI 过滤失效
- **类型**：趋势跟随
- **指标**：EMA、RSI
- **时间框架**：5 分钟（默认）
