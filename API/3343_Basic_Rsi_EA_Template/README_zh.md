# 基础 RSI EA 模板策略
[English](README.md) | [Русский](README_ru.md)

**Basic RSI EA Template Strategy** 为 MetaTrader 4 专家顾问 “Basic Rsi EA Template.mq4”（MQL/26750）的 StockSharp 版本。策略在指定的K线序列上监控相对强弱指数（RSI），当动量进入自定义的超买或超卖区域时执行交易。转换保留了原始机器人“单一持仓+固定止损止盈”的简洁风格，并采用 StockSharp 的高级订阅接口实现。

## 策略逻辑

### 指标
- **RSI**：周期可配置，基于所选 K 线类型计算。

### 入场条件
- **做多**：当 RSI 低于 `OversoldLevel` 且当前没有持仓时，以 `OrderVolume` 的固定数量市价买入。
- **做空**：当 RSI 高于 `OverboughtLevel` 且当前没有持仓时，以 `OrderVolume` 的固定数量市价卖出。

策略使用净头寸模式，同一时间只允许一笔仓位。如果持有多单，则等待其平仓后才允许做空（反之亦然）。

### 出场条件
- **止损**：`StopLossPips` 根据品种最小跳动价转换为绝对价格距离；当价格不利运行达到该距离时，通过保护模块平仓。
- **止盈**：`TakeProfitPips` 以同样方式转换；当价格向有利方向移动达到目标距离时平仓获利。

没有额外的移动止损或信号反向平仓逻辑，策略完全依赖预设的保护距离或人工干预，与原始模板保持一致。

### 风险与仓位管理
- `OrderVolume` 设置每次市价单的固定交易量（默认 0.01 手，与 MQL 示例一致）。
- 策略不加仓、不对冲。止损或止盈触发后，重新等待下一次 RSI 信号。

## 参数说明
- `CandleType`：用于计算信号的 K 线类型（默认 1 分钟）。
- `RsiPeriod`：RSI 计算窗口长度（默认 14）。
- `OverboughtLevel`：RSI 超买阈值（默认 70）。
- `OversoldLevel`：RSI 超卖阈值（默认 30）。
- `StopLossPips`：止损距离（点），内部转换为绝对价格（默认 30 点）。
- `TakeProfitPips`：止盈距离（点），内部转换为绝对价格（默认 20 点）。
- `OrderVolume`：每笔市价单的固定数量（默认 0.01）。

## 实现细节
- 通过 `SubscribeCandles(...).Bind(rsi, ProcessCandle)` 获取指标数值，无需手动管理缓冲区。
- `CreateProtectionUnit` 模拟原始 MQL 对点值的处理：报价保留 3 或 5 位小数的品种使用 10 倍乘数将点数映射为价格步长。
- 仅在 K 线收盘后评估信号，避免同一根 K 线上重复下单。
- 转换基于净头寸账户，与 MetaTrader 的对冲模式不同，因此反向交易会先平掉当前仓位。
- 代码中的注释与日志全部使用英文，便于国际化维护。

## 使用建议
- 根据交易周期调整 `CandleType`（例如切换到小时线进行波段交易）。
- 根据标的波动率调节 `StopLossPips` 与 `TakeProfitPips`，它们是风险控制的关键。
- 如需更复杂的资金管理，可在该模板基础上接入 StockSharp 的组合或风险管理模块。
