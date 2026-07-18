# Covid統計トラッカー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

COVID-19確認感染者数の増加率に基づいてトレードする戦略です。
感染者数の増加が加速したときに売り、増加が鈍化したときに買います。

## 詳細

- **エントリー条件**:
  - ロング: `growth < 1`
  - ショート: `growth > 1`
- **ロング/ショート**: 両方
- **エグジット条件**: 反対シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `Region` = "US"
  - `Lookback` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: その他
  - 方向: 両方
  - インジケーター: カスタム
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
