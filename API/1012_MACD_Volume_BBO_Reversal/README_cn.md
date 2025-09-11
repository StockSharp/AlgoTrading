# MACD Volume BBO Reversal 策略

该策略结合成交量振荡器和 MACD 穿越零轴以及信号线关系。
当 MACD 上穿零轴且成交量振荡器为正并且 MACD 高于信号线时做多，
做空条件相反。止损设置在最近的低点或高点，
止盈按照风险收益比计算。

## 参数
- `VolumeShortLength` – 成交量短期 EMA 周期 (默认 6)
- `VolumeLongLength` – 成交量长期 EMA 周期 (默认 12)
- `MacdFastLength` – MACD 快速周期 (默认 11)
- `MacdSlowLength` – MACD 慢速周期 (默认 21)
- `MacdSignalLength` – MACD 信号线周期 (默认 10)
- `LookbackPeriod` – 计算最近高低点的K线数 (默认 10)
- `RiskReward` – 止盈/止损比率 (默认 1.5)
- `CandleType` – K线时间框架 (默认 5 分钟)
