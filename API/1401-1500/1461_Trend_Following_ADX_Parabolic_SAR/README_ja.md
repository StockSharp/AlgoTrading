# トレンドフォロー戦略 ADX Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ADX と方向性移動指数および Parabolic SAR を使用してトレンドに追従します。ADX が閾値を上回り、+DI が -DI を超え、価格が SAR ラインを上回るときにロングポジションを取ります。ショートシグナルは逆の条件を使用します。

## 詳細

- **エントリー条件**: ADX > 閾値、DI クロスオーバー、価格 > SAR。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ADX, Parabolic SAR
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
