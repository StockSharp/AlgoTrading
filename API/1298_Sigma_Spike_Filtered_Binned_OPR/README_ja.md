# シグマスパイクフィルタリング済みビン分類 OPR 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Sigma Spike Filtered Binned OPR は建玉比率（OPR）の分布を収集し、リターンにシグマスパイクが発生した後に OPR が極端なビンに達したときに取引を行います。

## 詳細

- **エントリー条件**: OPR が極端なビン内にある (<= `OprThreshold` または >= `100 - OprThreshold`) で、オプションのシグマスパイクフィルターを使用
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のシグナル
- **ストップ**: なし
- **デフォルト値**:
  - `SigmaSpikeLength` = 20
  - `FilterBySigmaSpike` = true
  - `SigmaSpikeThreshold` = 2
  - `OprThreshold` = 10
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: StandardDeviation
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
