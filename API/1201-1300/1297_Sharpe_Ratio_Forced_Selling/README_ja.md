# Sharpe Ratio 強制売却戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Sharpe Ratio 強制売却戦略は、ローリング Sharpe Ratio が負のしきい値を下回ったときにロングエントリーし、正のしきい値を上回ったとき、または保有期間が上限を超えたときにイグジットします。リターンは対数変化または単純変化で計算でき、リスクフリーレートで調整できます。

## 詳細

- **エントリー条件**: Sharpe Ratio が `EntrySharpeThreshold` を下回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: Sharpe Ratio が `ExitSharpeThreshold` を上回る、または保有期間が `MaxHoldingDays` を超える。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 8
  - `EntrySharpeThreshold` = -5
  - `ExitSharpeThreshold` = 13
  - `MaxHoldingDays` = 80
  - `UseLogReturns` = true
  - `RiskFreeRateAnnual` = 0
  - `PeriodsPerYear` = 252
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング
  - インジケーター: Sharpe Ratio
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
