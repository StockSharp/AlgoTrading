# StellarLite ICT EA 策略

## 概述
StellarLite ICT EA 策略把 "Stellar Lite" 挑战的人工主观流程迁移到 StockSharp 框架中。策略整合了 ICT (Inner Circle Trader) 的 Silver Bullet 与 2022 Model 两种入场模板，并自动执行原始 MetaTrader EA 中的分批止盈、保本和跟踪止损管理。

## 核心流程
1. **高周期方向判断**：在高周期蜡烛上计算移动平均线。当均线向交易方向倾斜且收盘价位于均线上方/下方时，才允许进入下一步分析。
2. **流动性扫单验证**：在可配置窗口内寻找最近高点或低点的扫单。Silver Bullet 需要沿着交易方向扫单，而 2022 Model 需要先出现反方向的诱导扫单。
3. **结构转变 (MSS)**：最近三根完成蜡烛必须证明结构突破——多头需要收盘价创出上一根高点，空头则需要收盘价跌破上一根低点。
4. **公平价值缺口 (FVG)**：扫描最近十根蜡烛是否存在由动量蜡烛形成的看涨/看跌缺口，且当前收盘价必须落在该缺口内部。
5. **窄幅波动过滤 (NDOG/NWOG)**：当前蜡烛的高低价差必须小于 `AtrThreshold × ATR`，以确认市场处于压缩状态。
6. **入场、止损与目标**：入场价位于缺口中位或根据斐波那契 OTE 比例计算。止损放在近期流动性点之外，三组止盈按照设定的风险回报比计算。
7. **仓位管理**：根据风险百分比或策略 Volume 计算头寸大小。TP1、TP2、TP3 触发时分别平仓 50%、25%、25%，TP1 后可选地移动止损至保本并加上偏移量，TP2 后启动跟踪止损，若触发 TP3 或触发止损则清空剩余仓位。

## 参数
- **Entry Candle (`CandleType`)** – 触发信号的低周期蜡烛类型。
- **Higher Timeframe (`HigherTimeframeType`)** – 用于方向判定的高周期蜡烛类型。
- **Higher MA Period (`HigherMaPeriod`)** – 高周期均线周期。
- **ATR Period (`AtrPeriod`)** – ATR 指标的回溯长度。
- **Liquidity Lookback (`LiquidityLookback`)** – 搜索流动性池的蜡烛数量。
- **ATR Threshold (`AtrThreshold`)** – 蜡烛允许的最大波幅（相对于 ATR 的倍数）。
- **TP1/TP2/TP3 Risk Reward (`Tp1Ratio`, `Tp2Ratio`, `Tp3Ratio`)** – 各级止盈的风险回报倍数。
- **TP1/TP2/TP3 Close % (`Tp1Percent`, `Tp2Percent`, `Tp3Percent`)** – 各级止盈的平仓比例。
- **Break Even After TP1 (`MoveToBreakEven`)** – TP1 后是否移动止损到保本。
- **Break Even Offset (`BreakEvenOffset`)** – 保本止损的额外价格步进数量。
- **Trailing Distance (`TrailingDistance`)** – TP2 之后启动的跟踪止损步进。
- **Use Silver Bullet / Use 2022 Model (`UseSilverBullet`, `Use2022Model`)** – 是否启用对应模板。
- **Use OTE Entry (`UseOteEntry`)** – 是否使用 OTE 回撤区间计算入场价。
- **Risk % (`RiskPercent`)** – 每笔交易的账户风险百分比，用于计算手数。
- **OTE Lower (`OteLowerLevel`)** – OTE 区间的斐波那契比例。

## 使用建议
- 策略仅在完成蜡烛上运作，需要行情源提供收盘价与最小价格步长/手数信息。
- 若无法获取投资组合市值或最小变动价值，则退回到策略的 `Volume` 设置计算手数。
- 流动性与 MSS 逻辑依赖最近 20 根蜡烛缓存，启动后需要等待数据累积。
- 分批平仓会遵循交易品种的最小手数，如果比例过小将跳过执行。
- 跟踪止损仅向盈利方向移动，不会放宽现有风险控制。

## 文件
- `CS/StellarLiteIctEaStrategy.cs` – 策略代码实现。
- `README.md` – 英文说明。
- `README_cn.md` – 中文说明。
- `README_ru.md` – 俄文说明。
