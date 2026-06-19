# McClellan A-D 量能整合模型策略
[English](README.md) | [Русский](README_ru.md)

该策略通过将K线的价格差乘以成交量构建加权的 A-D 线，并对其计算两条 EMA 形成 McClellan 振荡器。

当振荡器从下方上穿设定阈值时开多仓，持仓达到指定的K线数量后自动平仓。

## 细节

- **入场**：振荡器从下向上突破 `Long Entry Threshold`。
- **离场**：持仓达到 `Exit After Bars` 根K线后平仓。
- **多空**：仅做多。
- **指标**：两条 EMA。
- **止损**：无。
- **周期**：可配置。
