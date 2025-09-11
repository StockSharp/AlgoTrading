# MESA Stochastic 多周期策略
[English](README.md) | [Русский](README_ru.md)

该策略使用四个不同周期的 MESA Stochastic 振荡器。当所有振荡器都位于其移动平均触发线之上时开多；当所有振荡器都位于触发线之下时开空。

## 参数
- `Length1` – 第一个振荡器周期。
- `Length2` – 第二个振荡器周期。
- `Length3` – 第三个振荡器周期。
- `Length4` – 第四个振荡器周期。
- `TriggerLength` – 触发线平滑周期。
- `CandleType` – K 线时间框架。
