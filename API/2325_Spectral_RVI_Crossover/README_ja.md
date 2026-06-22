# Spectral RVIクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Spectral RVI Crossover戦略は、Relative Vigor Indexとそのシグナルラインを平滑化し、それらのクロスオーバーで取引します。
平滑化されたRVIが平滑化されたシグナルラインを上抜けると買い、反対のクロスが発生すると売ります。

## 詳細

- **エントリー条件**: 平滑化RVIが平滑化シグナルラインとクロス
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のクロスオーバー
- **ストップ**: いいえ
- **デフォルト値**:
  - `RviLength` = 14
  - `SignalLength` = 4
  - `SmoothLength` = 20
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RVI、SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 4時間
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
