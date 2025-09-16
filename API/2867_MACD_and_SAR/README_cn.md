# MACD and SAR
[English](README.md) | [Русский](README_ru.md)

该策略复刻了 MetaTrader 上的 “MACD and SAR” 专家顾问。在每根完成的 K 线结束后，它会同时检查 MACD 主线与信号线之间的关系以及 Parabolic SAR 点位。每个比较条件都提供了布尔开关，可以轻松反转方向，因此同一个框架既可用于顺势也可用于逆势思路。当累积仓位数未超过上限时，允许分批加仓。

当出现做多信号时，策略会先平掉所有空头头寸，然后在允许的情况下新增一个多头手数。做空信号的处理方式相同：先平多，再开空。没有额外的止损或止盈订单，仓位只会在生成反向信号时被关闭。

## 策略逻辑

1. 等待配置好的时间框架上的 K 线收盘。
2. 读取基于收盘价计算的 MACD（三个分量）以及当前的 Parabolic SAR 数值。
3. 按以下顺序检查三个比较条件，每一项都可以通过布尔参数翻转：
   - MACD 主线与信号线的大小关系。
   - MACD 信号线与零轴的关系。
   - Parabolic SAR 与收盘价的关系。
4. 如果三个做多条件全部满足且尚未达到持仓上限，则买入设定的手数（同时覆盖平空所需的数量）。
5. 如果三个做空条件全部满足且仍有空间，则卖出设定的手数（包括平多所需的数量）。

## 参数

- `TradeVolume` — 单笔交易的成交量（默认 `0.1`）。
- `MaxPositions` — 同方向最多允许累计的仓位数量（默认 `10`）。
- `MacdFastPeriod` — MACD 快速 EMA 周期（默认 `12`）。
- `MacdSlowPeriod` — MACD 慢速 EMA 周期（默认 `26`）。
- `MacdSignalPeriod` — MACD 信号线平滑周期（默认 `9`）。
- `SarStep` — Parabolic SAR 加速度步长（默认 `0.02`）。
- `SarMaximum` — Parabolic SAR 最大加速度（默认 `0.2`）。
- `BuyMacdGreaterSignal` — 若为 `true`，做多需要 MACD 主线 > 信号线；否则要求主线 < 信号线（默认 `true`）。
- `BuySignalPositive` — 若为 `true`，做多需要信号线 > 0；否则要求信号线 < 0（默认 `false`）。
- `BuySarAbovePrice` — 若为 `true`，做多要求 SAR 高于价格；否则要求价格高于 SAR（默认 `false`）。
- `SellMacdGreaterSignal` — 若为 `true`，做空需要 MACD 主线 > 信号线；否则要求主线 < 信号线（默认 `false`）。
- `SellSignalPositive` — 若为 `true`，做空需要信号线 > 0；否则要求信号线 < 0（默认 `true`）。
- `SellSarAbovePrice` — 若为 `true`，做空要求 SAR 高于价格；否则要求价格高于 SAR（默认 `true`）。
- `CandleType` — 指标计算所使用的 K 线类型/时间框架（默认 15 分钟）。

## 其他说明

- 策略完全依赖指标信号，不包含额外的止损、止盈或资金管理规则。
- 通过比较 `|Position|` 与 `MaxPositions * TradeVolume`（带有微小容差）来控制加仓上限。
- 所有交易均以市价执行，请确保组合的成交量设置与目标市场兼容。
- 如需限制回撤或添加移动止损，可以利用 StockSharp 的保护机制，本实现未内置相关功能。

