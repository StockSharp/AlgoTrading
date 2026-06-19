# TrendManager TM Plus 策略

## 概述
TrendManager TM Plus 策略来自 MetaTrader 5 平台上的 `Exp_TrendManager_Tm_Plus.mq5` 智能交易系统。该策略使用 TrendManager 指标对两条平滑移动平均线之间的差值进行监控。当差值超过阈值时判定趋势方向，并在趋势反转或风险控制条件触发时平仓。

## 交易逻辑
1. 在所选的 K 线序列上计算两条移动平均线，用户可以为快线和慢线分别选择平滑算法和周期长度。
2. 计算快线与慢线的距离。当距离大于等于阈值时认定为上升趋势；当距离小于等于负阈值时认定为下降趋势；否则视为无信号。
3. 将颜色状态（0 表示多头，1 表示空头，3 表示中性）保存到短历史中，`SignalBar` 参数决定回溯多少根收盘 K 线来确认新信号，完全复刻 MQL 的判断方式。
4. 新的多头颜色出现时，可选地平掉已有空头，并在允许的情况下开多；新的空头颜色出现时，可选地平掉已有多头，并在允许的情况下开空。
5. 如果启用了时间或价格风控，则在持仓超过 `MaxPositionAge`、价格触发 `StopLossDistance` / `TakeProfitDistance` 时提前离场。

## 参数
- **Candle Type**：用于计算信号的 K 线类型，默认使用 4 小时周期以保持与原脚本一致。
- **Fast/Slow MA Method**：快线与慢线的平滑算法，可选简单、指数、平滑、加权、Jurik 以及 Kaufman 自适应等方法。
- **Fast/Slow Length**：两条移动平均线的周期。
- **Distance Threshold (`DvLimit`)**：触发趋势所需的最小绝对距离。若要从 MT5 的点值换算，可将点数乘以品种的最小价格步长（例如 5 位报价的 70 点 ≈ 0.00070）。
- **Signal Bar**：回溯的 K 线数量，用于确认新信号，默认值 1 与原版相同。
- **Allow Long/Short Entries**：分别控制是否允许做多或做空。
- **Close Long/Short on Opposite Signal**：对立信号出现时立即平掉现有仓位。
- **Use Time Exit / Max Position Age**：是否启用时间止损，以及最大持仓时间。
- **Order Volume**：下单固定手数，用于替代 MT5 脚本中的资金管理模块。
- **Stop Loss Distance / Take Profit Distance**：可选的止损与止盈距离，单位为价格，设置为 0 表示关闭。

## 实现要点
- 使用 StockSharp 自带的移动平均指标还原 TrendManager 行为。若原指标中的平滑方法在 StockSharp 中不存在，会自动回退到最接近的可用算法。
- 信号处理保存了一个小型历史缓存，以便 `SignalBar` 与原脚本一样检测颜色切换。
- 止盈止损依据收盘 K 线的最高价/最低价来判断，模拟 MT5 实盘中可能出现的盘中触发。
- 原脚本中的 `Deviation`、保证金模式等参数已改为更适合 StockSharp 的设置。

## 使用建议
1. 根据交易周期选择合适的 Candle Type；若希望与原版一致，保持 H4 即可。
2. 根据标的的波动性调整阈值，波动越大的品种需要更大的 `DvLimit`。
3. 结合时间退出与止损/止盈距离，可重现原 MT5 策略的风险控制。
4. 若需要多空切换，保持多空同时允许，并在对立信号出现时使用自动反向功能。

## 与原版 EA 的差异
- 订单手数使用固定的 `OrderVolume`，不再调用 MT5 的资金管理函数。
- 止损与止盈通过 K 线数据模拟执行，不会直接在交易所挂单。
- 使用 StockSharp 的原生移动平均指标，部分平滑选项直接映射，若无对应实现则采用最接近的算法。
- 时间退出使用 `TimeSpan` 类型的 `MaxPositionAge`，替代原脚本中的分钟数配置。

以上内容描述了在 StockSharp 环境下配置与扩展 TrendManager TM Plus 策略所需的全部关键信息。
