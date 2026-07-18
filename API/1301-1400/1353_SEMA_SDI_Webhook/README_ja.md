# SEMA SDI Webhook 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

平滑化された EMA クロスオーバーと平滑化された方向性指標の確認に基づく戦略。
+DI > -DI かつ短期 EMA > 長期 EMA のときに買い、-DI > +DI かつ短期 EMA < 長期 EMA のときに売ります。

## 詳細

- **エントリー条件**:
  - ロング: `+DI > -DI && FastEMA > SlowEMA`
  - ショート: `+DI < -DI && FastEMA < SlowEMA`
- **ロング/ショート**: 両方
- **エグジット条件**: テイクプロフィット、ストップロス、トレーリング
- **ストップ**: TP、SL、トレーリング
- **デフォルト値**:
  - `FastEmaLength` = 58
  - `SlowEmaLength` = 70
  - `SmoothLength` = 3
  - `DiLength` = 1
  - `TakeProfitPercent` = 25
  - `StopLossPercent` = 4.8
  - `TrailingPercent` = 1.9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, Directional Index
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
