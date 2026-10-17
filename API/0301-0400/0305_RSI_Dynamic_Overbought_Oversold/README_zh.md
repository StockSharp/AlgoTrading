# RSI Dynamic Overbought Oversold 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

结合价格趋势过滤，按自适应超买/超卖边界交易 RSI 极值。使用已完成5分钟 K 线。计算 `R = RSI(14)`、收盘价的 `P = SMA(50)`，以及 R 的 `A = SMA(50)`、`D = StandardDeviation(50)`。边界为 `upper = min(A + 2 × D, 85)`、`lower = max(A − 2 × D, 15)`。所有指标必须形成。

空仓时，`R <= lower && Close >= P × 0.995` 买入，`R >= upper && Close <= P × 1.005` 卖出。`R >= A || Close < P × 0.995` 平全部多仓；`R <= A || Close > P × 1.005` 平全部空仓。每笔信号订单后暂停 `CooldownBars = 48` 根已完成 K 线的后续信号判断。

已成交持仓具有本地原生百分比保护：`StopLossPercent = 2`，以实际入场成交价为基准。监控标的的 bid/ask，并独立于信号冷却提交市价平仓；K 线之间需要报价流。无止盈。

`RsiPeriod = 14; MovingAvgPeriod = 50; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 48`.
