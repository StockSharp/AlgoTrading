# TSI DeMarker戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

DeMarkerオシレーターの上にTrue Strength Indexを計算する戦略です。
TSIが移動平均シグナルラインを上抜けするとロングポジションがオープンされます。
TSIがシグナルラインを下抜けするとショートポジションがオープンされます。

このアプローチはモメンタム分析と買われすぎ/売られすぎ分析を組み合わせます。

## 詳細

- **エントリー条件**:
  - ロング: `TSIがシグナルを上抜け`
  - ショート: `TSIがシグナルを下抜け`
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のシグナル
- **ストップ**: なし
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
  - `DemarkerPeriod` = 25
  - `ShortLength` = 5
  - `LongLength` = 8
  - `SignalLength` = 20
- **フィルター**:
  - カテゴリ: オシレータークロスオーバー
  - 方向: 両方
  - インジケーター: TSI, DeMarker
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
