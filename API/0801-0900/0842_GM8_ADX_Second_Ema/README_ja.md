# GM-8とADXおよび第2EMAを用いた戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格がGM-8 SMAをクロスし第2EMAと整合し、ADXが強いトレンドを確認した際にトレードを行います。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格がSMAを上抜けし、SMAと第2EMAの両方を上回るクローズとなり、ADXが閾値を超えている。
  - **ショート**: 価格がSMAを下抜けし、SMAと第2EMAの両方を下回るクローズとなり、ADXが閾値を超えている。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: 価格がSMAを下抜け。
  - **ショート**: 価格がSMAを上抜け。
- **ストップ**: StartProtectionを使用。
- **デフォルト値**:
  - `GM Period` = 15
  - `Second EMA Period` = 59
  - `ADX Period` = 8
  - `ADX Threshold` = 34
  - `Candle Type` = 15m
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA, EMA, ADX
  - ストップ: はい
  - 複雑さ: 低
  - 時間軸: 短期

