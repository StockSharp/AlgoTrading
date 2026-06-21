# Chaikinモメンタム・スキャルピング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このスキャルピング戦略はChaikinオシレーターを使用してモメンタムの変化を捉えます。オシレーターがゼロを上抜けし、価格が200期間SMAの上にある場合にロングを建てます。ゼロを下抜けし価格がSMAの下にある場合にショートを建てます。ATRの倍数でストップロスとテイクプロフィットのレベルを定義します。

## 詳細

- **エントリー条件**: Chaikinオシレーターがゼロを上/下抜けし、価格がSMAの上/下にある。
- **ロング/ショート**: 両方。
- **エグジット条件**: ATRベースのストップロスとテイクプロフィット。
- **ストップ**: はい。
- **デフォルト値**:
  - `FastLength` = 3
  - `SlowLength` = 10
  - `SmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplierSL` = 1.5m
  - `AtrMultiplierTP` = 2.0m
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: Momentum
  - 方向: 両方
  - インジケーター: Chaikin Oscillator, SMA, ATR
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
