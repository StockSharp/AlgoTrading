# Two-Pole Ideal MA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高速EMAと低速TEMAを比較することで「2pb Ideal MA」エキスパートを近似するクロスオーバーシステムです。

## 詳細

- **エントリー条件**: 高速EMAが低速TEMAをクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のクロスオーバーで反転。
- **ストップ**: なし。
- **デフォルト値**:
  - `FastPeriod` = 10
  - `SlowPeriod` = 30
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, TEMA
  - ストップ: なし
  - 複雑さ: 初心者
  - 時間軸: スイング (H4)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
