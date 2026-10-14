# Previous Day High Low Long
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

只做多：在UTC入场时段，close上穿前一UTC日的观测high或low，且原生ADX上升。通过实际成交价利润目标、观测价格跟踪止损或时段结束退出。

## 水平与入场

只用已完成蜡烛，默认五分钟。按UTC开盘日期汇总high/low，在下个日期开始时固定前日观测最高high和最低low。前日首根观测蜡烛必须从午夜开始且日期连续；初始部分日期或缺失日期不提供虚构水平。当前日蜡烛不扩大前日水平。

high信号：`previousClose <= previousDayHigh && close > previousDayHigh`；low同样向上穿越前日low。原生`AdxLength=11`的ADX必须形成且严格大于前一形成值，无额外ADX阈值或DI方向过滤。

从空仓发`Volume`市价买单，提交UTC时刻在`[SessionStart,SessionEnd)`内，默认09:30–16:00。每根完成蜡烛更新close、日和ADX，被拒绝穿越不延期。平仓后的新穿越可再入场，无日次入场限制或六小时冷却，不开空。

## 保护与结束

`MaxProfit=150`、`MaxStopLoss=15`是绝对价格距离，不是百分比或价格步长。实际买入成交初始化entryPrice和highWater；追加部分买入更新成交量加权entryPrice，减仓保留基础，全平重置。

每个正的可执行bid或已完成close将highWater更新为历史值与当前价的最大值。价格≤`highWater-MaxStopLoss`或≥`entryPrice+MaxProfit`时市价卖出实际多头全量。优先级是时段结束、跟踪止损、利润目标，不将蜡烛high当作可执行报价。

每分钟市场时间定时器、bid和完成蜡烛在/超过`SessionEnd`或下个UTC日期关闭持仓。时段需在同一UTC日按顺序设置；00:00–24:00在下个午夜结束。未完成订单阻止重复，日/ADX历史继续更新；在蜡烛回调中关闭后直接返回，不再次入场。

本地监控不发送交易所原生止损。盘中报价保护需要Level1 bid，无报价仍检查close；成交可偏离阈值。

| 参数 | 默认值 |
|---|---:|
| `MaxProfit` / `MaxStopLoss` | 150 / 15，正的绝对价格距离 |
| `AdxLength` | 11，正 |
| `SessionStart` / `SessionEnd` | 09:30 / 16:00 UTC，含起点、不含终点 |
| `CandleType` | 5分钟 |

C#/Python在现有BTC/TON历史独立汇总日期、计算Wilder ATR/DI/DX/ADX，检查每个实际入场/退出、两个前日水平、ADX拒绝、初始部分历史、实际成交、报价止损/目标及强制时段关闭，不证明盈利。
