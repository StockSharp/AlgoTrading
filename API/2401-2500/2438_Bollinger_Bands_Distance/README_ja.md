# Bollinger Bands 距離戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

追加の距離フィルターを使用したBollinger Bandsのリバーサル取引戦略です。価格がアッパーバンドに設定された距離を加えた値より上で終値を付けると売り、ロワーバンドから同じ距離を引いた値より下で終値を付けると買います。ポジションは価格ステップで測定された利益目標またはストップロスで決済されます。

## 詳細

- **エントリー条件**:
  - ロング: Bollinger Bandsの下限バンドから距離を引いた値より下で終値
  - ショート: Bollinger Bandsの上限バンドに距離を加えた値より上で終値
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 利益目標達成
  - ストップロス達成
- **ストップ**: 価格ステップの絶対値
- **デフォルト値**:
  - `BollingerPeriod` = 4
  - `BollingerDeviation` = 2m
  - `BandDistance` = 3m
  - `ProfitTarget` = 3m
  - `LossLimit` = 20m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
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
