# ARD注文管理戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

DeMarkerインジケーターが0.5のしきい値を突破したときにポジションを建てる戦略。

DeMarkerが上方から下方にしきい値を割り込んだ場合、戦略は買いを入れます。DeMarkerが下方から上方にしきい値を超えた場合は売ります。逆のシグナルで決済します。ストップロスもテイクプロフィットも使用しません。

## 詳細

- **エントリー条件**:
  - ロング: `DeMarkerがThresholdを上から下に突破`
  - ショート: `DeMarkerがThresholdを下から上に突破`
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `DeMarkerPeriod` = 2
  - `Threshold` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: インジケーター
  - 方向: 両方
  - インジケーター: DeMarker
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
