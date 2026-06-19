# AMkA 信号策略

## 概述

该策略结合考夫曼自适应移动平均线 (KAMA) 的变化率和基于标准差的波动率过滤器。当 KAMA 的变化超过正阈值时开多仓，当变化低于负阈值时开空仓。阈值通过将 KAMA 变化的标准差乘以用户设定的倍数得到。

## 参数

- **KAMA Length** – KAMA 指标的回溯周期。
- **Fast Period** – KAMA 快速 EMA 的周期。
- **Slow Period** – KAMA 慢速 EMA 的周期。
- **Deviation Multiplier** – 与标准差相乘形成信号阈值的倍数。
- **Take Profit** – 盈利百分比。
- **Stop Loss** – 止损百分比。
- **Candle Type** – 用于计算的蜡烛图时间框架。

## 交易逻辑

1. 订阅所选时间框架的蜡烛数据。
2. 计算 KAMA 并与上一值比较得到变化量。
3. 使用变化量更新标准差指标。
4. 当变化量超过 `Deviation Multiplier * StdDev` 时：
   - 变化量大于阈值：平空仓并开多仓。
   - 变化量小于负阈值：平多仓并开空仓。
5. 通过 `StartProtection` 自动管理止盈和止损。

## 备注

策略仅处理已完成的蜡烛。代码使用制表符缩进，所有注释均为英文。
