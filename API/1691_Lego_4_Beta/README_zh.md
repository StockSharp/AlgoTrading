# Lego 4 Beta 策略

该策略来自 MetaTrader 脚本“exp_Lego_4_Beta”的改写，是一个模块化系统，可通过参数开启或关闭不同的技术指标组件。

## 算法

1. **均线交叉** – 计算快慢两条移动平均线。快线向上穿越慢线时开多，反向穿越时开空。
2. **随机指标过滤** – 启用后，多头需要随机指标 %K 低于超卖阈值，空头需要 %K 高于超买阈值。
3. **RSI 平仓** – 启用后，多头在 RSI 超过高阈值时平仓，空头在 RSI 低于低阈值时平仓。

## 参数

- `UseMaOpen` – 启动均线交叉信号。
- `FastMaLength` / `SlowMaLength` – 快慢均线长度。
- `MaType` – 均线类型（SMA、EMA、WMA）。
- `UseStochasticOpen` – 启用随机指标过滤。
- `StochLength` – 随机指标主周期。
- `StochKPeriod` / `StochDPeriod` – %K 和 %D 平滑周期。
- `StochBuyLevel` / `StochSellLevel` – 超卖与超买阈值。
- `UseRsiClose` – 启用 RSI 平仓。
- `RsiPeriod` – RSI 计算周期。
- `RsiHigh` / `RsiLow` – RSI 平仓阈值。
- `CandleType` – 订阅的蜡烛类型。

## 说明

策略使用高级 `SubscribeCandles` 与 `BindEx` 获取指标值，符合 StockSharp 推荐的编码风格，仅使用市价单进出场。
