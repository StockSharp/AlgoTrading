# Two X SPY TIPS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、S&P 500 と TIPS の価格がともに 200 期間移動平均を上回っている状態で月の変わり目を迎えたとき、取引対象資産に資金を配分します。

## 詳細

- **エントリー条件**: 新しい月の開始時に S&P 500 と TIPS が SMA を上回っている。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: エグジットなし。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `SmaLength` = 200
  - `Leverage` = 2
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: はい
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
