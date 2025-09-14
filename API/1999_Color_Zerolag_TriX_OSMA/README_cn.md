# Color Zerolag TriX OSMA 策略

## 概述

该策略使用由五个不同 TRIX 周期组成的零滞后 TRIX OSMA 振荡器。每个 TRIX 分量根据权重进行平滑处理，形成一个能快速响应趋势变化的振荡器。当振荡器向上转折时开多头仓位，向下转折时开空头仓位。

## 工作原理

1. 使用三重指数移动平均和变化率计算五个 TRIX 值。
2. 将 TRIX 值按权重组合得到快速趋势值。
3. 对快速趋势进行两次平滑以生成零滞后 OSMA 振荡器。
4. 比较最近两个振荡器值以检测趋势反转。
5. 振荡器向上转折时先平掉空头再开多头，向下转折时相反。

## 参数

- `Smoothing1` – 慢速趋势的平滑系数。
- `Smoothing2` – OSMA 线的平滑系数。
- `Factor1..Factor5` – 各 TRIX 分量的权重。
- `Period1..Period5` – 五个 TRIX 的周期。
- `CandleType` – 用于计算的 K 线类型。

## 指标

- TripleExponentialMovingAverage
- RateOfChange
- 自定义零滞后 TRIX OSMA 组合

## 备注

策略在所有五个 TRIX 指标形成后才产生信号。通过 `StartProtection` 启用止损和止盈保护。
