# 強化版Bollinger Bands SL TP戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bollinger Bandsのバウンスを指値注文で取引し、固定ピップベースのストップロスとテイクプロフィットを使用する戦略。

## 詳細

- **エントリー条件**:
  - ロング: 前回終値 <= 前回下限バンド かつ 終値 > 下限バンド
  - ショート: 前回終値 >= 前回上限バンド かつ 終値 < 上限バンド
- **ロング/ショート**: 両方
- **ストップ**: ピップ単位の絶対的なテイクプロフィットとストップロス
- **デフォルト値**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2m
  - `EnableLong` = true
  - `EnableShort` = true
  - `PipValue` = 0.0001m
  - `StopLossPips` = 10m
  - `TakeProfitPips` = 20m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: Bollinger Bands
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
