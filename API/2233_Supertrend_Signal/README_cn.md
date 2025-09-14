# Supertrend Signal 策略
[English](README.md) | [Русский](README_ru.md)

当收盘价穿越 SuperTrend 线时，本策略开仓。价格上破该线时做多，跌破该线时做空。相反的信号会平掉并反向已有仓位。

SuperTrend 指标基于平均真实波幅 (ATR)，跟随价格以确定主导趋势。参数可设置 ATR 周期、倍数以及K线时间框架。

## 细节

- **入场条件**：
  - 多头：收盘价向上突破 SuperTrend 线
  - 空头：收盘价向下跌破 SuperTrend 线
- **多/空**：双向
- **出场条件**：
  - 相反的 SuperTrend 突破
- **止损**：无
- **默认值**：
  - `AtrPeriod` = 5
  - `Multiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **筛选**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SuperTrend（基于 ATR）
  - 止损：否
  - 复杂度：入门
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中
