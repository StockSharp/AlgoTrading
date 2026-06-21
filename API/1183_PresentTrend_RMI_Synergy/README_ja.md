# PresentTrend RMI Synergy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

PresentTrend RMI SynergyはRSIベースのモメンタムフィルターとSuperTrendスタイルのATRトレーリングストップを組み合わせています。モメンタムが閾値を超え、価格がトレンドと一致したときにエントリーが発生します。ストップは移動平均とATRバンドを使用して価格を動的に追跡します。

バックテストでは、暗号通貨などのトレンド市場で安定したパフォーマンスを示しています。

## 詳細

- **エントリー条件**: ロングは移動平均より上の価格でRMIが60超え；ショートは移動平均より下の価格でRMIが40未満。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRベースのトレーリングストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `RmiPeriod` = 21
  - `SuperTrendLength` = 5
  - `SuperTrendMultiplier` = 4.0m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: RSI, ATR, SMA
  - Stops: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
