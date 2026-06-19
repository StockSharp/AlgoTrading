# Virtual TradePad Signal 策略
[English](README.md) | [Русский](README_ru.md)

本策略将 MetaTrader 中的 VirtualTradePad 指标面板移植到 StockSharp。系统会监控 12 个趋势、通道与动量指标，
只有在达到设定数量的同向信号时才进场，从而把原始面板的情绪矩阵转化为自动化交易逻辑。

## 工作原理

- **数据**：在选定的蜡烛类型（默认 15 分钟）上交易单一品种。
- **指标**：
  - 快速/慢速简单移动平均的金叉死叉。
  - MACD 主线与信号线交叉。
  - 随机指标 %K 穿越 20/80 区域。
  - RSI 穿越 30/70。
  - CCI 穿越 -100/+100。
  - Williams %R 穿越 -80/-20。
  - 价格重新回到布林带内部。
  - 价格重新回到 SMA 百分比通道内部。
  - 比尔·威廉姆斯鳄鱼线（颚、牙、唇）排列方向。
  - Kaufman 自适应均线的斜率（上升/下降）。
  - Awesome Oscillator 穿越零轴。
  - Ichimoku Tenkan 与 Kijun 的交叉。
- 每个指标都会产生 +1（做多）、-1（做空）或 0（中性）的投票。当多头（或空头）投票数量达到
  **MinimumConfirmations** 参数并且多于反方向时，策略将开仓。
- `CloseOnOpposite` 选项会在反向投票达到阈值时平仓。
- **风险控制**：可选的止盈/止损，单位为品种的价格步长。

## 参数

- `FastMaLength`, `SlowMaLength` —— 均线周期。
- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` —— MACD 设置。
- `StochasticLength`, `StochasticDLength`, `StochasticSlowing` —— 随机指标设置。
- `RsiLength`, `CciLength`, `WilliamsLength` —— 震荡指标周期。
- `BollingerLength`, `BollingerDeviation` —— 布林带参数。
- `EnvelopeLength`, `EnvelopeDeviation` —— SMA 百分比通道宽度。
- `AlligatorJawLength`, `AlligatorTeethLength`, `AlligatorLipsLength` —— 鳄鱼线 SMMA 周期。
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` —— Kaufman AMA 设置。
- `IchimokuTenkanLength`, `IchimokuKijunLength`, `IchimokuSenkouLength` —— Ichimoku 参数。
- `AoShortPeriod`, `AoLongPeriod` —— Awesome Oscillator 周期。
- `MinimumConfirmations` —— 进场所需同向信号数量。
- `AllowLong`, `AllowShort` —— 是否允许做多/做空。
- `CloseOnOpposite` —— 反向信号达到阈值时是否平仓。
- `TakeProfitPips`, `StopLossPips` —— 以价格步长表示的止盈/止损（0 表示禁用）。
- `CandleType` —— 使用的蜡烛类型或时间框。

## 交易流程

1. 每根蜡烛收盘时计算所有指标。
2. 统计多头与空头票数。
3. 当票数满足阈值并且领先于反方向时开仓。
4. 如果启用 `CloseOnOpposite`，在反向票数满足阈值时平仓。
5. 根据需要应用止盈与止损。

该策略适用于喜欢 VirtualTradePad 情绪面板、同时希望在 StockSharp 中获得自动化实现的交易者。
