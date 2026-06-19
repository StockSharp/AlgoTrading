# 三红三绿策略（带 ATR 过滤）

在连续三根阴线且 ATR 高于其 30 期平均时开多；在连续三根阳线或持仓时间达到上限时平仓。

## 参数

- **CandleType**：K 线类型。
- **MaxTradeDuration**：最大持仓 K 线数量。
- **UseGreenExit**：是否在连续三根阳线后平仓。
- **AtrPeriod**：ATR 计算周期（0 表示禁用过滤）。
