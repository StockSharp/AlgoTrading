# 2pb Ideal MA 重新开仓策略

## 概述
- 基于 StockSharp 高级 API 重写 MQL 专家顾问 “Exp_2pbIdealMA_ReOpen”。
- 通过单阶段与三阶段理想移动平均线的反向交叉来进行交易。
- 当价格按设定的跳动数继续向盈利方向运行时加仓，并可在反向信号出现时平仓。

## 指标
- **2pb Ideal 1 MA**：具有两个权重周期的单阶段理想移动平均线，用于刻画短期节奏。
- **2pb Ideal 3 MA**：同一理想滤波器的三级串联（X、Y、Z 阶段），反应更慢，用于刻画背景趋势。

## 交易逻辑
1. 订阅所选周期的K线（默认 H4），仅在K线收盘后进行计算。
2. 保存 `SignalBarShift` 根历史数据（默认 1），使用 `SignalBarShift` 与 `SignalBarShift + 1` 这两根的滤波值判定交叉。
3. **做多开仓**：若两根前快线高于慢线，而上一根快线跌破慢线（看跌交叉），在允许做多且当前无持仓时买入。
4. **做空开仓**：若两根前快线低于慢线，而上一根快线上穿慢线（看涨交叉），在允许做空且当前无持仓时卖出。
5. **加仓**：持仓盈利时，价格每移动 `PriceStepTicks * Security.PriceStep` 的距离，追加一笔 `PositionVolume` 的订单。每个方向的加仓次数受 `MaxReEntries` 限制。
6. **离场**：当出现反向交叉且对应的平仓开关开启时，先行平掉当前仓位再评估新的入场机会。
7. 依据设定的跳动数应用可选的止损与止盈保护。

## 参数
- `CandleType`：参与计算的K线类型/周期。
- `PositionVolume`：每次初始入场及加仓使用的基础手数，同时会赋值给 `Strategy.Volume`。
- `StopLossTicks` / `TakeProfitTicks`：以跳动数表示的止损、止盈距离，通过 `Security.PriceStep` 转换为价格。
- `PriceStepTicks`：两次加仓之间要求的最小跳动数。
- `MaxReEntries`：每个方向允许的最大加仓次数。
- `EnableBuyEntries` / `EnableSellEntries`：允许开多或开空。
- `EnableBuyExits` / `EnableSellExits`：当出现反向信号时是否关闭现有仓位。
- `SignalBarShift`：参与交叉判断的回溯K线数，对应原策略的 `SignalBar`。
- `Period1`, `Period2`：单阶段理想移动平均线的权重参数。
- `PeriodX1`, `PeriodX2`, `PeriodY1`, `PeriodY2`, `PeriodZ1`, `PeriodZ2`：三阶段理想移动平均线各级的权重参数。

## 风险管理
- 当止损或止盈跳动数大于零时，通过 `StartProtection` 激活保护单。
- 策略在存在相反方向仓位时不会开立新仓，与原 MQL 逻辑保持一致。

## 说明
- 适用于任何提供 `Security.PriceStep` 的交易品种，默认配置针对 H4 周期。
- 根据要求未提供 Python 版本。
