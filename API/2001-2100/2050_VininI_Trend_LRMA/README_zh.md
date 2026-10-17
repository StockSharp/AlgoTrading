# VininI Trend LRMA 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

跟踪已完成 K 线收盘价滚动线性回归的端点 `L`。默认 `CandleType = 4小时`、`Period = 13`、`UpLevel = 10`、`DnLevel = -10`、`Mode = Breakdown`。等待指标形成并允许交易。

**Breakdown** 的阈值用于相对振荡值 `O = 100 × (Close − L) / L`，单位为百分比。`previousO <= UpLevel && O > UpLevel` 时买入；`previousO >= DnLevel && O < DnLevel` 时卖出。阈值不是 LRMA 的绝对价格水平。

**Twist** 在回归端点斜率由负转正时买入，由正转负时卖出。平坦段保留此前的非零方向。

买入要求 `Position <= 0`，卖出要求 `Position >= 0`。先用市价单平掉相反的已成交持仓，再以 `Volume` 开新方向。同向信号不加仓。无止损或止盈。
