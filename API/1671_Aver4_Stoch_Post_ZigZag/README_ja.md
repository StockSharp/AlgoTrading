# Aver4 Stoch Post ZigZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複数の時間軸にわたる4つのStochオシレーターとシンプルなZigZagピボット検出器を組み合わせます。平均Stochが売られすぎ/買われすぎのレベルを誘導し、ZigZagがスイングの高値と安値を確認します。平均StochがOversoldレベルを下回り、新しいZigZag安値が形成されたときに買い。平均StochがOverboughtレベルを上回り、新しいZigZag高値が形成されたときに売り。シグナルが逆転すると既存の逆方向ポジションはクローズされます。

## 詳細
- **エントリー条件**: 平均StochがOversold/Overboughtゾーンを通過し、対応するZigZagピボットが存在する。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: StartProtection 2%/2%（デフォルト）。
- **デフォルト値**:
  - `ShortLength` = 26
  - `MidLength1` = 72
  - `MidLength2` = 144
  - `LongLength` = 288
  - `ZigZagDepth` = 14
  - `Oversold` = 5
  - `Overbought` = 95
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Stochastic, ZigZag
  - ストップ: はい
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
