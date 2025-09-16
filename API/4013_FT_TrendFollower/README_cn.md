# FT 趋势跟随

## 概述
FT 趋势跟随是 MetaTrader 4 智能交易系统 `FT_TrendFollower.mq4` 的 StockSharp 移植版本。策略通过叠加古比均线 (GMMA) 扇形、Laguerre 振荡器触发器、快/慢 EMA 金叉过滤器以及 MACD 主线过滤，在中等周期的趋势中寻找入场点。只有当价格先跌入 GMMA 束内部、在 Laguerre 极值处反弹，并且大多数 GMMA 曲线重新朝交易方向倾斜时，才会触发开仓。风险控制完全复刻原始 EA：可选的摆动止损、固定距离止损，以及三种互斥的分批离场模块（基于日枢轴位或波动通道）。

## 策略逻辑
### GMMA 结构与趋势判定
* GMMA 从 `StartGmmaPeriod` 延伸到 `EndGmmaPeriod`。周期被分成五组，每组包含 `BandsPerGroup` 条均线，对应原版参数 `CountLine`。
* 趋势方向通过比较长周期组中较慢的 GMMA（倒数 `CountLine + CountLine` 条）与较快的长周期 GMMA（倒数 `CountLine` 条）来确定。较慢均线位于较快均线之上视为上升趋势，反之为下降趋势。
* 斜率确认会统计短、中、长三组 GMMA 与前一根 K 线相比是上升还是下降。只有当上升（或下降）计数超过 GMMA 总数的一半时，信号才被允许通过，对应 MT4 中的 `controlvverh`/`controlvverhS` 判断。

### 信号预备条件
* **收盘复位** – 当前 K 线收盘价跌破最慢 GMMA 时，做多模块进入待命状态；收盘价升破最慢 GMMA 时，做空模块待命。当价格重新越过最快 GMMA 时，相应的待命标志会被清除，对应原始的 `CloseOk` 逻辑。
* **Laguerre 触发器** – 在满足长周期 GMMA 约束的前提下，Laguerre 值必须先跌破 `LaguerreOversold`（做多）或升破 `LaguerreOverbought`（做空）。只有当振荡器重新穿回阈值上方（或下方）时，信号才会释放。
* **EMA 金叉过滤器** – 做多时，快 EMA（`FastSignalLength`）必须先跌破慢 EMA（`SlowSignalLength`）后再上穿。做空逻辑相反。
* **MACD 过滤** – MACD 主线（5/35/5 组合）做多需为正值，做空需为负值。

### 入场条件
做多订单在以下条件同时满足时触发：
1. 趋势判定为上升趋势，且 GMMA 斜率投票超过总数的一半。
2. Laguerre 触发器处于待命状态，并且当前值重新站上 `LaguerreOversold`。
3. 快 EMA 上穿慢 EMA。
4. MACD 主线大于零。

做空条件完全对称：振荡器需要从 `LaguerreOverbought` 上方向下穿越，且 MACD 小于零。若存在反向持仓，策略会自动补齐反向仓位，使得最终净仓等于 `Volume` 参数。

### 风险管理与离场
* **止损** – 可选择摆动止损（`UseSwingStop`），将止损设在上一根 K 线的高/低点外 `SwingStopPips` 个点；或选择固定距离止损（`UseFixedStop`），距离为 `FixedStopPips` 个点。当两者同时启用时，策略会在启动阶段抛出异常，与原始 EA 的校验一致。
* **枢轴位离场模块（Quit）** – 启用后，当价格突破上一交易日的 R1/S1 且头寸处于浮盈状态时，会先平仓一半仓位。其余仓位在 Hull 均线（`HmaPeriod`）输出有效值后立即离场，对应 MT4 中 `hma1` 缓冲区的检查。
* **枢轴区间离场模块（Quit1）** – 第一笔减仓仍在 R1/S1 触发，剩余仓位在价格触及 R2/S2 且仍处于盈利状态时全部离场。
* **通道离场模块（Quit2）** – 同样先在 R1/S1 减仓一半。当下一根 K 线开盘价跌破低位 SMA 通道（多单）或升破高位通道（空单）时，平掉余下仓位，对应原策略的波动率过滤器。

三个离场模块互斥，只能启用其中之一，这一点继承自原 EA 的参数约束。

## 参数
* **Volume** – 开仓手数。
* **StartGmmaPeriod / EndGmmaPeriod** – GMMA 扇形的起止周期。
* **BandsPerGroup** – 每组采样的 GMMA 条数（MT4 中的 CountLine）。
* **FastSignalLength / SlowSignalLength** – EMA 金叉过滤器的周期。
* **TradeShift** – 为兼容原版而保留，只允许 0 或 1；策略使用完结 K 线计算。
* **UseSwingStop / SwingStopPips** – 启用及设置摆动止损距离。
* **UseFixedStop / FixedStopPips** – 启用固定距离止损，单位为价格点。
* **EnablePivotExit / EnablePivotRangeExit / EnableChannelExit** – 三个互斥的分批离场模块。
* **LaguerreOversold / LaguerreOverbought / LaguerreGamma** – Laguerre 触发器的阈值与平滑因子。
* **HmaPeriod** – Hull MA 周期，用于枢轴离场模块。
* **ChannelPeriod** – 高/低 SMA 通道的周期，用于 Quit2。
* **CandleType** – 主时间框架（默认 1 小时）。

## 补充说明
* 日枢轴位使用独立的日线订阅，取上一根日线的高低收计算。
* 所有点值换算基于标的的 `PriceStep`，可自动适配不同报价精度。
* 策略完全基于高阶 API 的指标绑定，不直接访问指标缓冲区。
* 本目录不提供 Python 版本。
