# Berlin Range Index戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Berlin Range Index戦略は、ATRベースのファクターで標準のChoppiness Indexをフィルタリングし、トレンドとレンジ局面を浮き彫りにします。フィルタリングされたインデックスが最小閾値を下回ると、戦略は現在のローソク足の方向にポジションを開きます。インデックスがレンジまたは弱まるトレンドを示すときにポジションはクローズされます。

## 詳細

- **エントリー条件**:
  - フィルタリングされたレンジインデックスが`ChopMin`を下回り、ローソク足の方向がロングまたはショートを決定。
- **エグジット条件**:
  - レンジインデックスが`ChopMax`を上回るか、トレンドが弱まる。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 9
  - `ChopMax` = 40
  - `ChopMin` = 10
  - `AtrLength` = 14
  - `LowLookback` = 14
  - `UseNormalized` = true
  - `StdDevLength` = 14
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Choppiness Index, ATR, Standard Deviation
  - 複雑さ: 中
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
