# BreakOut15 策略

## 概述
BreakOut15 策略由 MetaTrader 4 的 "BreakOut15.mq4" 专家顾问转换而来，适用于 15 分钟周期。策略通过高低周期均线交叉来确认趋势，再等待价格突破设定的距离后入场，并使用多阶段移动止损来保护利润。全部订单都通过 StockSharp 的高级 API 提交，只处理收盘完成的 K 线。

## 交易逻辑
1. 根据参数设置计算快、慢两条移动平均线（方法、周期、位移、价格类型均可配置）。
2. 当快线向上穿越慢线时，记录一个潜在的多头突破价 `Close + BreakoutLevel * PriceStep`；向下穿越时记录空头突破价 `Close - BreakoutLevel * PriceStep`。
3. 若交叉条件消失、交易时段结束或出现反向突破信号，等待中的突破价将被取消。
4. 当后续 K 线突破相应价格，且账户权益与风险控制允许时，以市价开仓。
5. 持仓期间由固定止损/止盈和三种可选的追踪止损模式进行风险控制。一旦均线再次反向交叉，会立即平仓。
6. 可以设置交易时段限制，并在周五收盘前强制平掉所有仓位。

## 资金管理
* **UseMoneyManagement / TradeSizePercent** – 启用后，仓位根据权益百分比计算：`floor(equity * percent / 10000) / 10` 的整数部分，最少 1 手。
* **FixedVolume** – 在未启用资金管理或无法取得权益时使用的固定手数。
* **MaxVolume** – 限制最大下单手数。
* **MinimumEquity** – 账户权益低于该值时不再开新仓。

## 风险管理
* **StopLossPips / TakeProfitPips** – 以点数定义的固定止损和止盈（根据合约最小价格变动转换）。
* **UseTrailingStop** – 启动动态移动止损。
* **TrailingStopType**
  * `Immediate`：入场后立即按初始止损距离移动止损。
  * `Delayed`：盈利达到 `TrailingStopPips` 后才开始以该距离跟踪。
  * `MultiLevel`：在三个触发点（`Level1/2/3TriggerPips`）逐级锁定利润，最终以 `Level3TrailingPips` 的距离跟踪。

## 交易时段控制
* **UseTimeLimit, StartHour, StopHour** – 限定允许开新仓的小时区间。
* **UseFridayClose, FridayCloseHour** – 可选择在周五指定时间平仓离场。

## 指标与数据
* **Fast/Slow moving averages** – 支持简单、指数、平滑、加权及最小二乘等多种均线算法。
* **Applied price** – 覆盖 MT4 的所有价格类型（收盘、开盘、最高、最低、中位、典型、加权）。
* **CandleType** – 默认使用 15 分钟蜡烛，可按需调整。

## 其他说明
* 策略会自动将入场价、止损和止盈与当前持仓均价同步，确保追踪止损基于真实成交价格。
* 请确认交易品种的 `PriceStep` 设置正确，因为所有点数换算都依赖该值。
* 建议测试不同的突破场景、追踪止损模式切换以及资金管理取整逻辑，以验证策略表现。
