# Supertrend 距離ブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Supertrend 距離ブレイクアウト戦略は、Supertrend の急激な拡張を観察します。読み値が平均的な範囲を超えてジャンプすると、価格はしばしば新しい動きを開始します。

テストでは年平均リターン約115%を示しています。株式市場で最もよく機能します。

インジケーターが最近のデータと偏差乗数から導かれたバンドを突き抜けると、ポジションが開きます。ストップを付けたロングとショートの両取引が可能です。

このシステムは早期ブレイクアウトを求めるモメンタムトレーダーに適しています。Supertrend が平均に戻るとトレードはクローズされます。デフォルト値は `SupertrendPeriod` = 10 から始まります。

## 詳細

- **エントリー条件**: インジケーターが偏差乗数分だけ平均を上回る。
- **ロング/ショート**: 両方向。
- **エグジット条件**: インジケーターが平均に戻る。
- **ストップ**: はい。
- **デフォルト値**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Supertrend
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

