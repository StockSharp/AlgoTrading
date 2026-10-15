# EUR/USD 多層統計回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EUR/USDのトレンド方向を推定するために複数の線形回帰層を使用する戦略です。短期、中期、長期の回帰を計算し、R²と傾きの閾値で検証し、加重アンサンブルの方向に取引します。

## 詳細

- **エントリー条件**:
  - ロング: 加重傾き > 0 かつ信頼性 > 0.5
  - ショート: 加重傾き < 0 かつ信頼性 > 0.5
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のシグナルが現れたときに反転
- **ストップ**: 日次損失保護
- **デフォルト値**:
  - `ShortLength` = 20
  - `MediumLength` = 50
  - `LongLength` = 100
  - `MinRSquared` = 0.45m
  - `SlopeThreshold` = 0.00005m
  - `WeightShort` = 0.4m
  - `WeightMedium` = 0.35m
  - `WeightLong` = 0.25m
  - `PositionSizePct` = 50m
  - `MaxDailyLossPct` = 12m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Linear Regression
  - ストップ: はい
  - 複雑さ: 上級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - リスクレベル: 中
