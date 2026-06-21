# Kaufmanトレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Kaufmanトレンド戦略**はカルマンフィルターを使用して価格とモメンタムを推定します。トレンド強度はフィルターの速度成分から導出され、直近のウィンドウ内で正規化されます。強いトレンド条件がフィルター値の上下いずれかと一致するときにエントリーします。ストップは直近のスイング高値・安値にATRを加減したものに基づき、モメンタムが弱まるにつれて段階的に利益確定します。

## 詳細
- **エントリー条件**: トレンド強度のしきい値とフィルター値に対する価格の位置。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 段階的な利益確定とトレンド弱化またはストップ到達。
- **ストップ**: あり、スイング安値/高値 ± ATR。
- **デフォルト値**:
  - `TakeProfit1Percent = 50`
  - `TakeProfit2Percent = 25`
  - `TakeProfit3Percent = 25`
  - `SwingLookback = 10`
  - `AtrPeriod = 14`
  - `TrendStrengthEntry = 60`
  - `TrendStrengthExit = 40`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Kalman
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
