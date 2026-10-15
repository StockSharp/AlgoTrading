# Parabolic SAR 早期買い・MA ベース決済戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はParabolic SARインジケーターを使用し、インジケーターが価格に対して側面を切り替えたときに取引に入ります。単純移動平均が追加のエグジットルールを提供します。SARが価格の上にある状態で価格が移動平均を下回ったとき、ロングポジションを決済します。

## 詳細

- **エントリー条件**: SARが価格に対して側面を切り替える。
- **ロング/ショート**: 両方。
- **エグジット条件**: ロングポジションの場合、SAR > 価格かつ価格 < MAのとき決済。
- **ストップ**: 未定義。
- **デフォルト値**:
  - `Acceleration` = 0.02
  - `AccelerationStep` = 0.02
  - `MaxAcceleration` = 0.2
  - `MaPeriod` = 11
  - `CandleType` = 5分
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Parabolic SAR, SMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: なし
  - ニューラルネットワーク: なし
  - ダイバージェンス: なし
  - リスクレベル: 中
