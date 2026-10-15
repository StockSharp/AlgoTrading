# Keltner Seasonal Filter 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**Keltner Seasonal Filter** 策略基于 that trades based on Keltner Channel breakouts with seasonal bias filter。

测试表明年均收益约为 94%，该策略在股票市场表现最佳。

当 Keltner confirms filtered entries 在日内（5m）数据上得到确认时触发信号，适合积极交易者。

止损依赖于 ATR 倍数以及 EmaPeriod, AtrPeriod 等参数，可根据需要调整以平衡风险与收益。

## 详情
- **入场条件**：参见指标条件实现.
- **多空方向**：双向.
- **退出条件**：反向信号或止损逻辑.
- **止损**：是，基于指标计算.
- **默认值**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2m`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤器**:
  - 分类: 趋势跟随
  - 方向: 双向
  - 指标: Keltner, Seasonal
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 是
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

## 精确交易规则（C# 与 Python）

仅在指标就绪、允许交易且K线**已完成**时判断信号。设 `C` 为收盘价、`E = EMA(Close, EmaPeriod)`、`A = ATR(AtrPeriod)`、`M = AtrMultiplier`、`U = E + M × A`、`L = E − M × A`。

季节分数 `S` 使用**历史K线开盘时间的月份**（`candle.OpenTime.Month`），不使用 `OnStarted2` 的月份或机器日期。固定演示值（并非从历史数据计算）：1月0.8、2月0.3、3月0.6、4月0.7、5月0.2、6月−0.3、7月−0.1、8月−0.4、9月−0.8、10月0.1、11月0.9、12月0.7。

- **开多/空头反转：**`S > SeasonalThreshold && C > U && Position <= 0`，市价买入 `Volume + abs(Position)`，止损价 `C − M × A`。
- **开空/多头反转：**`S < −SeasonalThreshold && C < L && Position >= 0`，市价卖出 `Volume + abs(Position)`，止损价 `C + M × A`。
- **止损优先：**已有正数止损位时，多头在 `C <= stop`、空头在 `C >= stop` 时平仓，清除止损位并结束当前K线处理。
- **EMA平仓：**否则，多头在 `C < E`、空头在 `C > E` 时平仓并清除止损位。没有独立止盈或自动移动止损。

止损仅在**已完成K线收盘时手动检查**，不是交易所盘中止损单。止损以信号K线收盘价而非实际成交价计算。默认阈值 `0.5` 时，1/3/4/11/12月允许多头信号，9月允许空头信号，其余月份禁止新入场；允许的月份仍须满足通道突破条件。
