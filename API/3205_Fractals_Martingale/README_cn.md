# 分形马丁格尔策略

本目录提供 MetaTrader 智能交易系统“Fractals Martingale”的 StockSharp 高阶 API 版本。策略将比尔·威廉姆斯分形、基于
一目均衡表的趋势过滤器以及月度 MACD 确认结合在一起；持仓数量采用经典的马丁格尔序列，在连续亏损后放大仓位，
并通过冷却时间限制杠杆增长。

## 交易逻辑

1. **工作周期分形识别**：对已完成的 K 线建立缓冲区，寻找左右各 `FractalDepth` 根 K 线都更低/更高的局部高低点。
   如果下一根 K 线开盘价突破分形高点则记录多头机会；若开盘价跌破分形低点则记录空头机会。分形信号在
   `FractalLookback` 根处理过的 K 线内有效。
2. **一目均衡表趋势过滤**：分形信号必须与 `IchimokuCandleType` 所定义的更高周期一目均衡表趋势一致。多头需要
   转换线（Tenkan）高于基准线（Kijun），空头需要转换线低于基准线。
3. **月度 MACD 确认**：延续原始 EA 的做法，订阅 `MacdCandleType`（默认 30 天）周期的 MACD。只有当 MACD 主线
   位于信号线上方时才允许多头，空头则要求主线位于信号线下方。
4. **时段过滤**：仅在 `StartHour`（含）到 `EndHour`（不含）之间允许开仓，支持跨越午夜的交易窗口。
5. **马丁格尔加仓**：首单手数来自 `TradeVolume`。每次亏损后按 `Multiplier` 成倍放大下一笔订单的数量，并自动四舍
   五入到交易所的最小步长。盈利后重置到基准手数。当连续亏损次数超过 `MaxConsecutiveLosses` 时，策略暂停交易
   `PauseMinutes` 分钟后再以基准手数恢复。
6. **方向切换**：开新仓前会先平掉相反方向的持仓，保证净头寸与最新信号一致。

## 风险控制

- `StopLossPips` 与 `TakeProfitPips` 会根据检测到的点值转换为绝对价格距离，并通过 `StartProtection` 下发保护性止损
  和止盈，与原版 EA 的点差定义保持一致。
- 原策略提供的资金止盈/追踪逻辑在本移植中改为使用 StockSharp 自带的保护模块，因为真实账户的资金折算取决于券商
  实现。

## 参数说明

| 参数 | 描述 |
| --- | --- |
| `TradeVolume` | 序列第一笔订单的基础手数。 |
| `Multiplier` | 发生亏损后下一笔订单的倍数。 |
| `StopLossPips`、`TakeProfitPips` | 以点数表示的止损与止盈距离。 |
| `FractalDepth` | 确认分形所需的左右 K 线数量。 |
| `FractalLookback` | 分形信号保持有效的最大 K 线数量。 |
| `StartHour`、`EndHour` | 交易时段（交易所时间）。当两者相等时表示不过滤。 |
| `MaxConsecutiveLosses` | 触发冷却前允许的连续亏损次数。 |
| `PauseMinutes` | 触发冷却后的暂停时间（分钟）。 |
| `TenkanPeriod`、`KijunPeriod`、`SenkouPeriod` | 更高周期一目均衡表的各条线周期。 |
| `MacdFastPeriod`、`MacdSlowPeriod`、`MacdSignalPeriod` | MACD 快速、慢速与信号 EMA 的周期。 |
| `CandleType` | 主周期 K 线序列，用于分形检测与执行。 |
| `IchimokuCandleType` | 计算一目均衡表的更高周期。 |
| `MacdCandleType` | 计算 MACD 的周期（默认近似月线）。 |

## 使用提示

1. **点值计算**：点值来自 `Security.PriceStep`。对于五位报价的外汇品种，程序会自动乘以 10 以匹配 MetaTrader 的
   点差定义。
2. **多周期订阅**：策略最多同时订阅三组 K 线，请确认行情源能够提供所有所需的时间框架。
3. **控制马丁格尔风险**：连续翻倍会迅速放大风险。可通过降低 `Multiplier` 或缩短 `MaxConsecutiveLosses`/`PauseMinutes`
   来限制敞口。
4. **与 MT4 版本的差异**：邮件/推送提醒、账户资金止盈以及显式的保证金检查未在移植中保留，因为 StockSharp 已有
   对应的连接和风控机制。核心的入场、出场和加仓逻辑与原策略一致。

## 文件列表

- `CS/FractalsMartingaleStrategy.cs`：C# 实现，使用 StockSharp 高阶策略 API。
- `README.md`：英文说明。
- `README_cn.md`：本文件，简体中文说明。
- `README_ru.md`：俄文说明。
