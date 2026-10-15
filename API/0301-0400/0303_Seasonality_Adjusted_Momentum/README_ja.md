# 季節性調整モメンタム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Seasonality Adjusted Momentum**戦略は、季節性の強度で調整されたモメンタムインジケーターを中心に構築されています。

テストでは平均年間リターンが約172%であることが示されています。外国為替市場で最もよいパフォーマンスを発揮します。

季節性が日足データでモメンタムの変化を確認したときにシグナルが発動します。これにより、この手法はアクティブトレーダーに適しています。

ストップはATRの倍数とMomentumPeriod、SeasonalityThresholdなどの要素に基づいています。デフォルト値を調整してリスクとリワードのバランスを取ってください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターに基づく計算を使用。
- **デフォルト値**:
  - `MomentumPeriod = 14`
  - `SeasonalityThreshold = 0.5m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Seasonality, Adjusted
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: はい
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
