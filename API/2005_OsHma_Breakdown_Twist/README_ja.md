# OsHMA ブレイクアウト Twist
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

OsHMAオシレーター（高速と低速のHull Moving Averageの差分）に基づいた戦略です。2つのモードで動作できます:

- **Breakdown** – オシレーターがゼロラインを越えたときに取引します。
- **Twist** – オシレーターが方向を変えたときに取引します。

この戦略は選択した時間軸のローソク足を購読し、Hull Moving Averageインジケーターを使用してオシレーターを計算します。

## 詳細

- **エントリー条件**: OsHMAのゼロクロスまたは方向転換。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナルまたはストップ。
- **ストップ**: テイクプロフィットとストップロス。
- **デフォルト値**:
  - `FastHma` = 13
  - `SlowHma` = 26
  - `Mode` = Twist
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: H4
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
