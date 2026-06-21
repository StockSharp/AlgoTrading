# マルチ回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格が回帰ラインを超えたときに取引し、ボラティリティベースの境界でリスクを管理します。オプションのストップロスとテイクプロフィットのレベルは、選択したリスク指標から導出されます。

## 詳細

- **エントリー条件**: 価格が回帰値を上回るまたは下回ってクロスする。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナル、または価格が選択した境界に達したとき。
- **ストップ**: オプション、`UseStopLoss` および `UseTakeProfit` に基づく。
- **デフォルト値**:
  - `Length` = 90
  - `RiskMeasure` = Atr
  - `RiskMultiplier` = 1
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: LinearRegression, ATR/StdDev/Bollinger/Keltner
  - ストップ: オプション
  - 複雑さ: 中級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
