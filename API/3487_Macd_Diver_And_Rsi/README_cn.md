# MACD Diver and RSI 策略

## 概览

该策略是 **“Macd diver and rsi”** MetaTrader 5 智能交易系统的 C# 版本。策略保持原始的双阶段信号设计：RSI 负责识别超买或超卖极值，而 MACD 直方图用于确认动能是否重新转向。多头与空头拥有独立的参数，因此可以分别微调多空逻辑。

策略只订阅一个时间框架的蜡烛（可配置），并通过市价单直接交易所选标的。指标更新通过 `BindEx` 的高级 API 实现，满足项目指南。

## 交易逻辑

1. **指标初始化**
   - 创建两个 RSI 指标，分别用于多头与空头信号，具有独立周期和阈值。
   - 创建两个 `MovingAverageConvergenceDivergenceSignal` 指标，按照多头和空头的 MACD 设定运行。它们的直方图分量用于检测动能反转。
2. **入场条件**
   - **多头**：当多头 RSI 低于（或等于）超卖阈值，同时多头 MACD 直方图由负转正，即可开多。如果当前持有空头，则在同一笔市价单中平仓并反手做多。
   - **空头**：当空头 RSI 高于（或等于）超买阈值，同时空头 MACD 直方图由正转负，即可开空。若存在多头仓位，将先平掉再建立新的空头。
3. **风险控制**
   - 每次入场后，用信号蜡烛的收盘价作为参考价。
   - 根据多头与空头各自的止损/止盈参数，计算相对于参考价的价格水平。
   - 策略通过 `PriceStep` 把“点”（pip）转换为价格差，对 3 或 5 位小数的品种自动乘以 10，以贴近 MT5 的点值规则。
   - 每根完成的蜡烛都会检查高低价是否触及止损或止盈，一旦命中立即通过市价单平仓。
4. **仓位管理**
   - 当仓位归零（止损、止盈或反向信号导致）时，相关状态会被重置。
   - 策略不做分批减仓或移动止损，只使用固定的止损/止盈。

## 参数说明

- `CandleType`：用于计算信号的蜡烛时间框。
- `LongRsiPeriod`、`ShortRsiPeriod`：多头与空头 RSI 周期。
- `LongRsiThreshold`、`ShortRsiThreshold`：触发信号的 RSI 阈值（多头为超卖，空头为超买）。
- `LongMacdFastLength`、`LongMacdSlowLength`、`LongMacdSignalLength`：多头 MACD 的快/慢 EMA 以及信号 EMA 周期。
- `ShortMacdFastLength`、`ShortMacdSlowLength`、`ShortMacdSignalLength`：空头 MACD 的 EMA 设置。
- `LongVolume`、`ShortVolume`：每次开仓的交易量。若需要反手，会在同一笔订单中加上已有仓位的绝对值。
- `LongStopLossPips`、`LongTakeProfitPips`、`ShortStopLossPips`、`ShortTakeProfitPips`：多头与空头的止损/止盈点数，设置为 0 则禁用。

## 使用提示

- 请确保标的具有有效的 `PriceStep`，否则点值转换会退回默认的 0.0001。
- 由于多空指标独立，可对不同方向使用不同的过滤，例如收紧空头的超买阈值而保持多头更宽松。
- 代码包含英文注释，帮助理解策略流程并符合项目要求。
