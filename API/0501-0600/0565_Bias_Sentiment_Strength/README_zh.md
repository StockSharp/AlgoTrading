# Bias Sentiment Strength

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

在已完成的 15 分钟足上组合 MACD(12,26,9)、Wilder RSI(14)、随机指标(21,14,14)、Awesome Oscillator(5,34)、Alligator 与成交量加权偏差。原始 K 使用 21 根高低价；`Ks` 为其 14 值 SMA，`D` 再作 14 值 SMA。Alligator 使用中位价 SMMA：jaw 周期 13 位移 8，teeth 周期 8 位移 5，lips 周期 5 位移 3。

`BASS = 2 * (MACD - Signal) + (RSI - 50) / 5 + 0.15 * (Ks - D) + 0.6 * AO + (Lips - Teeth) + (Teeth - Jaw) + VWMA(30) - SMA(30)`。必须等所有部分形成；默认至少 47 根完整足。错误配置会明确报错。

分数严格大于 `BassThreshold` 时目标多仓 `Volume`，小于 `-BassThreshold` 时目标空仓 `Volume`，默认阈值为零。依据水平而非仅零轴交叉，止损后无需再次穿越零轴即可入场。反向用 `Volume + abs(Position)` 完整反转；活动订单阻止重复。原生本地市价止损按实际成交仓位的 `StopLossPercent` 百分比执行，默认 1，不设置止盈。

本地原生保护使用该品种的蜡烛和已订阅价格更新，距离以实际入场成交为基准。退出为市价订单，成交价受可用市场及价格步长影响。

## 参数

| 参数 | 默认值 |
|---|---|
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `RsiPeriod` | `14` |
| `StochK` | `21` |
| `StochSmooth` | `14` |
| `StochD` | `14` |
| `AoShort` | `5` |
| `AoLong` | `34` |
| `VolumeLength` | `30` |
| `BassThreshold` | `0` |
| `StopLossPercent` | `1` |
| `CandleType` | `15m` |
