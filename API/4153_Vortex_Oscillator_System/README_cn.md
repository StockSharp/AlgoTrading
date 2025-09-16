# 4153 Vortex振荡器系统

## 概述
该策略将 MetaTrader 4 的 “Vortex Oscillator System” 专家顾问迁移到 StockSharp 的高级 API。策略使用标准 Vortex 指标的 VI+ 与 VI- 分量构建归一化振荡器，并在动量突破自定义的中性区间时进行交易。算法只针对单一标的，在方向改变时始终完全平仓或反向开仓。

## 交易规则
- 根据 **CandleType** 订阅蜡烛数据，并以周期 **VortexLength** 计算 Vortex 指标。振荡器按照 `(VI+ - VI-) / (VI+ + VI-)` 计算，因此数值始终位于 `[-1, 1]` 区间。
- 当振荡器跌破 **BuyThreshold** 且（如果 **UseBuyStopLoss** 为真）仍高于 **BuyStopLossLevel** 时，激活做多准备。做空准备在振荡器升破 **SellThreshold** 且（若 **UseSellStopLoss** 为真）仍低于 **SellStopLossLevel** 时激活。
- 当振荡器重新回到 **BuyThreshold** 与 **SellThreshold** 构成的中性带内时，两个方向的信号都会被清除，下一次入场必须重新从中性状态开始。
- 激活的多头信号遇到空仓或空头仓位时，将以市价买入 **Volume** 手，并在存在空头仓位时自动加上需要平空的数量。空头信号采用镜像逻辑：市价卖出 **Volume** 手并附加当前多头仓位的数量。
- 不需要反向信号也可以退出仓位：启用 **UseBuyStopLoss** 后，振荡器触及 **BuyStopLossLevel** 会立即平掉多单；启用 **UseBuyTakeProfit** 后，振荡器超过 **BuyTakeProfitLevel** 将锁定多头利润。对应地，**SellStopLossLevel** 与 **SellTakeProfitLevel** 结合 **UseSellStopLoss**、**UseSellTakeProfit** 控制空头离场。

## 参数
- **VortexLength** – 用于计算 Vortex 指标的周期长度。
- **CandleType** – 请求行情数据的蜡烛类型或时间框架。
- **Volume** – 新开仓的基础手数；在反向操作时会自动加入平掉当前仓位所需的数量。
- **BuyThreshold** – 振荡器触发多头信号的阈值。
- **UseBuyStopLoss** – 要求振荡器在激活多头信号前保持在 **BuyStopLossLevel** 之上。
- **BuyStopLossLevel** – 启用止损后，多头在该振荡器水平被立即平仓。
- **UseBuyTakeProfit** – 启用基于振荡器的多头止盈。
- **BuyTakeProfitLevel** – 启用止盈后，多头在该振荡器水平实现盈利。
- **SellThreshold** – 振荡器触发空头信号的阈值。
- **UseSellStopLoss** – 要求振荡器在激活空头信号前保持在 **SellStopLossLevel** 之下。
- **SellStopLossLevel** – 启用止损后，空头在该振荡器水平被立即平仓。
- **UseSellTakeProfit** – 启用基于振荡器的空头止盈。
- **SellTakeProfitLevel** – 启用止盈后，空头在该振荡器水平实现盈利。

## 补充说明
- 策略会自动在图表上绘制蜡烛和成交，无需单独的振荡器面板即可观察运行情况。
- 振荡器已经归一化，因此默认阈值 `-0.75`、`0.75`、`-1.00` 与 `1.00` 可直接复用原始顾问的设定，并且可以通过 StockSharp 参数系统进行优化。
- 策略不允许多空同时持有，任何反向信号都会先平掉现有仓位，然后通过一笔市价单打开相反方向。
