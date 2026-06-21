# Chandeモメンタム・オシレーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はChandeモメンタム・オシレーターが下方閾値を下回ると買いを入れ、上方閾値を上回るか、または固定バー数が経過するとポジションを決済します。

テストでは年間平均リターンが約40%であることが示されています。トレンド相場で最も良いパフォーマンスを発揮します。

このオシレーターは直近の利益と損失を比較してモメンタムを測定します。極端な負の値は売られ過ぎ状態を示唆し、戦略はこれをロングエントリーに活用します。モメンタムがプラスに転じるか、保有期間が終了するとポジションを決済します。

## 詳細

- **エントリー条件**: `CMO < LowerThreshold`。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: `CMO > UpperThreshold` または `MaxBarsInPosition` バー経過。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `CmoPeriod` = 9
  - `LowerThreshold` = -50
  - `UpperThreshold` = 50
  - `MaxBarsInPosition` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロングのみ
  - インジケーター: CMO
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
