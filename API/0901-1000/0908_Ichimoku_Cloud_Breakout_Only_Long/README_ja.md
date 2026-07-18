# Ichimoku クラウドブレイクアウト ロングのみ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格がIchimokuクラウドを上抜けたときにロングポジションを建て、価格がクラウドを下回ったときに手仕舞います。ロングのみの取引を行います。

## 詳細

- **エントリー条件**:
  - ロング: `Close` が `max(SenkouA, SenkouB)` を上抜ける
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - `Close` が `min(SenkouA, SenkouB)` を下抜ける
- **ストップ**: なし
- **デフォルト値**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: Ichimoku
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
