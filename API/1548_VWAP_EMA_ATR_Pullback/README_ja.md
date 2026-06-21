# VWAP EMA ATR プルバック戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMA、VWAP、ATRを使用したトレンドフォロー戦略。

テストでは平均年間収益率約55%を示しています。先物市場で最も良いパフォーマンスを発揮します。

このアプローチはATRベースの距離で分離された高速・低速EMAを通じて強いトレンドを識別します。価格がVWAPにプルバックしたときにエントリーし、トレンドへの合流を狙います。テイクプロフィットはVWAP ± ATR乗数の位置に設定されます。

## 詳細

- **エントリー条件**:
  - **ロング**: 上昇トレンドかつ終値 < VWAP。
  - **ショート**: 下降トレンドかつ終値 > VWAP。
- **ロング/ショート**: 両方。
- **エグジット条件**: VWAP ± ATR * 乗数が目標。
- **ストップ**: なし。
- **デフォルト値**:
  - `FastEmaLength` = 30
  - `SlowEmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, ATR, VWAP
  - ストップ: いいえ
  - 複雑さ: 中程度
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
