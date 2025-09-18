# 通用信号演示
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 5 的 “Universal Signal” 专家顾问迁移到 StockSharp 的高级 API。它评估八个带权重的市场模式，将其合成为一个综合得分。当得分越过可配置的阈值时，策略会开仓或平仓多空头寸；如果需要，还会按设定距离提交限价挂单，并在若干根 K 线后自动取消。

## 策略参数
- `CandleType` – 用于分析的 K 线类型。
- `SignalThresholdOpen` – 开仓所需的最低综合得分。
- `SignalThresholdClose` – 触发平仓的反向得分阈值。
- `PriceLevel` – 挂单价差（0 表示按市价执行）。
- `StopLevel` / `TakeLevel` – `StartProtection` 使用的绝对止损与止盈距离。
- `SignalExpiration` – 未成交挂单的保留条数。
- `Pattern0Weight` … `Pattern7Weight` – 各个模式在总分中的权重。
- `UniversalWeight` – 应用于所有模式总和的全局系数。
- `ShortMaPeriod`, `LongMaPeriod`, `RsiPeriod`, `BollingerPeriod`, `BollingerWidth`, `TrendSmaPeriod`, `VolumeSmaPeriod` – 模式计算所依赖的指标参数。

## 交易逻辑
1. 订阅所选 K 线并绑定 EMA、RSI、MACD Signal、布林带及辅助均线。
2. 每根完成的 K 线都会计算八个布尔模式，例如趋势一致性、RSI 动能、MACD 柱体、布林带位置、K 线方向以及成交量放大等。
3. 每个模式与其权重相乘后求和，再乘以 `UniversalWeight` 得到最终得分。
4. 得分在反方向上越过平仓阈值时关闭已有头寸。
5. 得分超过开仓阈值时建立新仓位；若 `PriceLevel` 大于 0，则按设定距离提交限价单，并在 `SignalExpiration` 根 K 线后取消未成交的挂单。
6. 通过 `StartProtection` 为所有交易设置固定的止盈止损，沿用 StockSharp 的风险管理模块。

该移植版本保留了原始 MQL5 专家的灵活加权思想，同时遵循 StockSharp 的编码规范和指标驱动式流程。
