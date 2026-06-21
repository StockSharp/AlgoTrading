# XRVIクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

XRVIクロスオーバー戦略はExtended Relative Vigor Index（XRVI）に基づいています。
XRVIはRelative Vigor Indexを平滑化し、次にシグナルラインを生成するために第2の移動平均を適用することで計算されます。
戦略はXRVIがシグナルラインを上抜けするとロングに入り、下抜けするとショートに入ります。
既存のポジションは反対シグナルで反転されます。

## 詳細

- **エントリー条件**: XRVIとシグナルラインのクロス
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のクロスオーバー
- **ストップ**: なし
- **デフォルト値**:
  - `RviLength` = 10
  - `SignalLength` = 5
  - `CandleType` = H4時間軸
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Relative Vigor Index, Simple Moving Average
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
