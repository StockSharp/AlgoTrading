# VininI Trend LRMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VininI Trend LRMA戦略は線形回帰移動平均（LRMA）を使用して市場の方向性を追跡します。この戦略は2つのエントリーモードをサポートします。
- **Breakdown**: LRMAが固定された上位または下位レベルをクロスしたときに取引します。
- **Twist**: LRMAが方向を反転したときに取引します。

## 詳細

- **エントリー条件**: LRMAがレベルをクロス（Breakdown）または方向転換（Twist）
- **ロング/ショート**: 両方
- **エグジット条件**: 反対シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `CandleType` = TimeFrameCandle 4h
  - `Period` = 13
  - `UpLevel` = 10
  - `DnLevel` = -10
  - `Mode` = Breakdown
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: LinearRegression
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
