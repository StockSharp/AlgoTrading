# Levels With Trail 策略

该策略根据用户设定的价格水平进行交易，并可选择在盈利时跟踪止损。此实现基于 MQL 脚本 `levels_with_trail.mq4`。

## 工作原理
- 订阅所选时间框的蜡烛。
- 当没有持仓且收盘价上穿 `Level Price` 时买入，下穿时卖出。
- 若启用 `Trail Stop`，当行情向有利方向发展时，止损价会跟随移动。
- 持仓在达到止损、止盈或出现反向突破信号时平仓。

## 参数
- `Stop Loss` – 止损距离（价格单位）。
- `Take Profit` – 止盈距离（价格单位）。
- `Level Price` – 用于触发进场的价格水平。
- `Trail Stop` – 是否启用跟踪止损。
- `Candle Type` – 用于分析的蜡烛类型。
