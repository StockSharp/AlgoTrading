# Donchian 通道系统
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**Donchian 通道系统** 使用 Donchian 通道的突破信号并加入 `Shift` 参数以避免前瞻性偏差。

## 工作原理
- **做多**：当收盘价上穿 `Shift` 根K线前计算的 Donchian 上轨。
- **做空**：当收盘价下破 `Shift` 根K线前计算的 Donchian 下轨。
- 反向突破时头寸反转。

## 参数
- `DonchianPeriod` = 20
- `Shift` = 2
- `CandleType` = 4h

## 指标
- Donchian 通道
