# Auto RXD v1.67 策略

## 概述
Auto RXD v1.67 为同名 MetaTrader 智能交易系统的 StockSharp 版本。策略由三个线性感知机组成：监督感知机负责判定多空方向，另外两个分别寻找做多或做空机会。所有感知机都依赖线性加权移动平均（LWMA），该平均值根据 K 线收盘价以及 Robbie Ruan 提出的加权价格（高 + 低 + 2 × 收）计算。移植版本仅处理已经完成的 K 线，并使用高阶 `BindEx` 数据流确保指标与交易逻辑保持同步。

## 市场数据与指标
- **K 线** – 默认时间框架为 30 分钟，可通过 `CandleType` 参数修改。
- **平均真实波幅（ATR）** – 在 `UseAtrTargets` 为真时，为止盈和止损提供自适应距离，周期由 `AtrPeriod` 控制。
- **相对强弱指标（RSI）** – 可选过滤器。启用 `UseRsiFilter` 后，多头要求 RSI > 50，空头要求 RSI < 50。
- **商品通道指数（CCI）** – 可选趋势过滤器。在 `UseCciFilter` 为真时，多头需 CCI > +100，空头需 CCI < -100。
- **移动平均收敛散度（MACD）** – 可选动量过滤器。启用 `UseMacdFilter` 后，多头需要 MACD 线高于信号线，空头需要 MACD 线低于信号线。
- **平均趋向指标（ADX）** – 可选趋势强度过滤器。启用 `UseAdxFilter` 时需满足 ADX > `AdxThreshold`，同时 +DI / -DI 的方向与拟议交易一致。

## 交易逻辑
1. **感知机输入更新** – 每根 K 线都会把最新收盘价和加权价格写入缓存。缓存用于生成 LWMA 快照，并按照 `Step` 参数创建四个滞后特征，分别提供给多空感知机及监督感知机。
2. **监督判定** – 监督感知机依据 `SupervisorX1…X4` 权重和 `SupervisorThreshold` 偏置计算得分。得分 > 0 时启用做多感知机，得分 < 0 时启用做空感知机。若数据不足或得分为 0，则跳过该 K 线。
3. **方向感知机** – 被激活的感知机使用对应的权重（`LongX*` 或 `ShortX*`）再次评估。仅当得分为正时才进入下一步验证。
4. **指标过滤** – 当 `UseIndicatorFilters` 为假时，交易只由感知机信号驱动；为真时，每个启用的过滤器（RSI、CCI、MACD、ADX）都必须与信号方向一致，否则取消交易。
5. **下单** – 策略确保没有活跃委托，并平掉反向仓位。随后按 `OrderVolume` 下市价单，价格优先使用最佳报价，否则使用 K 线收盘价。

## 风险控制
- **防护委托** – 成交后立即调用 `CalculateProtectiveDistances` 计算止盈与止损距离。若 `UseAtrTargets` 启用，则距离等于 ATR × (`AtrTakeProfitFactor` 或 `AtrStopLossFactor`) × 原始点数。若关闭 ATR，则把固定点数转换为最小价位步长距离。
- **委托管理** – `SetProtectiveOrders` 将距离换算成价位步数并在入场价附近挂出止损与止盈，同时通过 `HasActiveOrders()` 避免重复下单。
- **StartProtection** – 在 `OnStarted` 中调用 `StartProtection()` 一次，利用框架自动在持仓出现时维护保护性委托。

## 参数
该实现完整继承 MQL 版本的参数，并根据用途进行分组，方便优化。

### 交易
- `OrderVolume` – 新建头寸的手数。
- `CandleType` – 绑定使用的 K 线类型。

### 风险
- `UseAtrTargets` – 切换 ATR 自适应或固定点数目标。
- `AtrPeriod`, `AtrTakeProfitFactor`, `AtrStopLossFactor` – ATR 相关设置。
- `LongTakeProfitPoints`, `LongStopLossPoints`, `ShortTakeProfitPoints`, `ShortStopLossPoints` – 原始止盈止损点数。

### 指标过滤器
- `UseIndicatorFilters` – 过滤器总开关。
- `UseAdxFilter`, `AdxPeriod`, `AdxThreshold` – ADX 设置。
- `UseMacdFilter`, `MacdFast`, `MacdSlow`, `MacdSignal` – MACD 设置。
- `UseRsiFilter`, `RsiPeriod` – RSI 设置。
- `UseCciFilter`, `CciPeriod` – CCI 设置。

### 感知机
- `ShortMaPeriod`, `ShortStep`, `ShortX1…ShortX4`, `ShortThreshold` – 做空感知机设置。
- `LongMaPeriod`, `LongStep`, `LongX1…LongX4`, `LongThreshold` – 做多感知机设置。
- `SupervisorMaPeriod`, `SupervisorStep`, `SupervisorX1…SupervisorX4`, `SupervisorThreshold` – 监督感知机设置。

所有参数默认值与原始 EA 保持一致，从而在 StockSharp 中重现相同的交易行为，并可借助 `StrategyParam` 系统进行批量优化。
