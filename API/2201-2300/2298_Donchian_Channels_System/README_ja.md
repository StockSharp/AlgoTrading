# Donchianチャネルシステム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Donchianチャネルシステム**戦略は、先読みバイアスを避けるためのオプションのシフトを使ってDonchianチャネルのブレイクアウトを取引します。

## 仕組み
- **ロングエントリー**: 終値が`Shift`バー前に計算されたDonchianの上限バンドを上抜けするとき。
- **ショートエントリー**: 終値が`Shift`バー前に計算されたDonchianの下限バンドを下抜けするとき。
- 逆方向のブレイクアウトでポジションが転換される。

## パラメーター
- `DonchianPeriod` = 20
- `Shift` = 2
- `CandleType` = 4h

## インジケーター
- Donchianチャネル
