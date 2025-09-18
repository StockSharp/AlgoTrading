# Mc Valute Cloud 策略

本目录提供 MetaTrader 专家顾问 “Mc_valute” 的 StockSharp 版本。原始 EA 通过一条短周期 EMA、三条平滑移动平均线、
Ichimoku 云以及多组 MACD 共同判断趋势，并在行情加速时逐步加仓。移植到 StockSharp 后保留了主要的趋势确认
模块，同时把持仓管理简化为每个方向只保留一笔仓位，使策略能够充分利用高层 API。

## 交易逻辑

1. **EMA 趋势过滤器** – `FilterMaLength` 指定的 EMA 需要位于两条平滑均线（`BlueMaLength` 与 `LimeMaLength`）之上
   才允许做多，位于其下才允许做空，这两条均线对应 MT4 模板中的蓝色和绿色线。
2. **Ichimoku 云过滤** – EMA 还必须突破 Ichimoku 云。做多时 EMA 要高于 Senkou Span A、B；做空时 EMA 要低于云层
   底部。
3. **MACD 动量确认** – 只有在 MACD 主线高于信号线时才开多，主线低于信号线时才开空。原 EA 中保留的第一组
   MACD 参数仍然生效，其余已在 MQL 源码中注释掉，因此移植版本也不再重复。
4. **单一仓位管理** – 产生新信号时，策略会平掉反向仓位，并以 `Volume` 指定的手数重新开仓，同时立即更新
   止盈和止损。
5. **仅使用已完成的 K 线** – 所有指标都运行在 `CandleType` 指定的周期上。交易决策只在 K 线收盘后执行，与
   MT4 `start()` 函数仅处理完结柱的行为一致。

## 风险控制

- `TakeProfit` 与 `StopLoss` 以价格点数表示。下单后会调用 `SetTakeProfit` 和 `SetStopLoss`，并按预期仓位规模设置
  防护水平，这与 MT4 中每笔订单分别设置止盈/止损的做法一致。
- 原版 EA 会按 `Step` 距离逐级加仓至三单。StockSharp 版本保持单一仓位，以便使用高层下单接口。如需加仓，
  可以调大 `Volume` 或并行运行多份策略实例。

## 参数

| 参数 | 说明 |
| --- | --- |
| `Volume` | 通过 `BuyMarket`/`SellMarket` 下单时使用的基础手数。 |
| `CandleType` | 计算指标与生成信号所使用的主时间框架。 |
| `FilterMaLength` | 趋势过滤 EMA 的周期。 |
| `BlueMaLength`, `LimeMaLength` | 两条平滑移动平均的周期。 |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD 主线与信号线的 EMA 周期。 |
| `TenkanLength`, `KijunLength`, `SenkouLength` | Ichimoku 云的 Tenkan、Kijun、Senkou 参数。 |
| `TakeProfit`, `StopLoss` | 止盈止损的点数距离。 |

## 使用说明

1. **移动平均的位移** – MT4 允许为平滑移动平均设置正负位移。StockSharp 的指标在当前柱上计算，因此忽略位移
   参数，仅保留原有周期。
2. **MACD 组合** – 源码声明了三组 MACD，但只有第一组参与信号。移植版本沿用该行为；若需要更多过滤条件，
   可以另外绑定新的 MACD 指标。
3. **分批加仓** – 原 EA 会按照 `Step` 距离加至最多三笔仓位。该差异已在文档记录，但在高层策略中刻意省略，
   因为系统只跟踪净头寸。
4. **保护模块** – 启动时调用 `StartProtection()`，确保在断线重连后止盈止损仍由框架自动维护。

## 文件结构

- `CS/McValuteCloudStrategy.cs` – 使用高层 Strategy API 编写的 C# 实现，并包含英文注释。
- `README.md` – 英文文档。
- `README_cn.md` – 中文文档（本文件）。
- `README_ru.md` – 俄文文档。
