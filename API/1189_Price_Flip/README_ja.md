# Price Flip戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Price Flip戦略は、直近の高値・安値を基準に価格を反転させ、前回終値がこの反転価格の反対側にあるときに移動平均クロスオーバーで取引します。遅い移動平均に基づくトレンドフィルターを適用できます。

## 詳細

- **エントリー条件**:
  - 前回の終値が反転価格を上回っている。
  - 速いMAが遅いMAを上抜く。
  - オプション: トレンドフィルターが有効な場合、価格が遅いMAを上回っている。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 反対シグナルで反転。
- **ストップ**: なし。
- **デフォルト値**:
  - `TickerMaxLookback` = 100
  - `TickerMinLookback` = 100
  - `FastMaLength` = 12
  - `SlowMaLength` = 14
  - `UseTrendFilter` = true
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA, Highest/Lowest
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
