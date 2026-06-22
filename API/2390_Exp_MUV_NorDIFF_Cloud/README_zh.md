# Exp MUV NorDIFF Cloud
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略基于SMA和EMA的归一化动量。
当SMA或EMA的动量达到+100时开多，降至-100时开空。

## 参数
- `MaPeriod` – 移动平均周期。
- `MomentumPeriod` – 计算动量的K线数量。
- `KPeriod` – 用于归一化动量极值的窗口。
- `CandleType` – K线时间框架。

## 说明
策略计算SMA和EMA的数值，测量其动量并在最近区间内归一化，
据此生成交易信号。
