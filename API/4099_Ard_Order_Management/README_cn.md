# 4099 Ard Order Management

## 概述
**Ard Order Management** 策略是 MetaTrader 专家顾问 `ARD_ORDER_MANAGEMENT_EA-BETA_1` 的 StockSharp 版本。原脚本的核心是先平掉现有持仓再重新下单，并提供手动修改止损/止盈的辅助方法。移植后的版本保留了这种严格的仓位管理，同时引入了基于随机指标的自动化信号。

默认配置面向 5 分钟外汇图表，但烛图类型可以自由调整。所有逻辑仅在收盘后执行，以保持与原 EA 相同的收盘价驱动风格。

## 交易逻辑
- 使用可调的随机指标（默认 5/3/3）生成方向信号。
- 当 %K 收于**买入阈值以上**（默认 80）时，先撤销挂单，再平掉空头，然后按设定手数做多。
- 当 %K 收于**卖出阈值以下**（默认 20）时，撤销挂单，平掉多头，再建立新的空头仓位。
- 持仓将一直保持，直到出现反向信号或触发保护性退出。

## 订单与风险管理
- 每次开仓前都会通过市价单把当前仓位完全反向，模拟原 EA 中 `open_order(CLOSE)` 的行为。
- `StartProtection` 根据 `StopLossPips` 与 `TakeProfitPips` 参数自动提交初始止损和止盈单。
- 可选的跟踪逻辑复刻了 EA 中的 `MODIFY` 分支：每根完结的 K 线都会重新计算跟踪止损 (`ModifyStopLossPips`) 和浮动止盈 (`ModifyTakeProfitPips`)。价格触发任一水平时立刻平仓，锁定盈利或限制风险。
- 点值换算基于标的物的 `PriceStep`，并对外汇常见的 1/10 点报价进行放大处理，保证不同品种之间的距离参数保持一致。

## 参数说明
- **Volume** – 新建仓位的交易量，若需反向，会自动加上对冲所需的数量。
- **TakeProfitPips / StopLossPips** – 初始止盈/止损距离（点）。设为 0 可以关闭对应的保护单。
- **ModifyTakeProfitPips / ModifyStopLossPips** – 跟踪止盈/止损的点数偏移。设为 0 即禁用跟踪。
- **StochasticPeriod / SignalPeriod / SlowingPeriod** – 随机指标的核心参数，对应原始 EA 中 `iStochastic` 的三个整型参数。
- **BuyThreshold / SellThreshold** – 触发多空反转的超买/超卖水平。
- **CandleType** – 用于计算指标的 K 线类型或时间框架。

所有参数都预设了合理的优化区间，可在 StockSharp 优化器中直接使用。

## 使用建议
- 适用于点值明确、流动性良好的品种（主流外汇、指数 CFD、流动性较好的期货）。
- 若市场节奏较慢，可提高时间框架以减少噪音信号。
- 实盘前请确认下单手数符合经纪商最小手数与步长要求。
- 若不需要动态调整，可将 `Modify*` 参数设为 0，得到与原 EA 类似的固定止损/止盈逻辑。
- 可以叠加趋势、波动率或交易时段过滤器，以提升信号质量；策略代码中的属性便于扩展。

## 移植要点
- 原始文件：`MQL/9041/ARD_ORDER_MANAGEMENT_EA-BETA_1.mq4`。
- 复现了 `start()` 函数中被注释的随机指标触发逻辑。
- 通过 StockSharp 高阶 API 保留了“先平仓再开仓”的纪律及保护性订单流程。
- 新增可选的跟踪退出机制，以事件驱动的方式还原 EA 中的手动 `MODIFY` 功能。
