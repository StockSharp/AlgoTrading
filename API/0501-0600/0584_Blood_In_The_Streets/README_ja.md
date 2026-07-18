# ストリートに血が流れる戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、直近の高値からの現在のドローダウンが標準偏差の閾値を下回ったときに買いを入れます。ポジションは固定本数のバー後に決済されます。

## 詳細

- **エントリー条件**:
  - ロング: ドローダウン ≤ 平均 + `StdDevThreshold` × 標準偏差
- **ロング/ショート**: ロングのみ
- **エグジット条件**: `ExitBars` 本のバー後にポジションを決済
- **ストップ**: なし
- **デフォルト値**:
  - `LookbackPeriod` = 50
  - `StdDevLength` = 50
  - `StdDevThreshold` = -1m
  - `ExitBars` = 35
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: ロング
  - インジケーター: Highest, SMA, StandardDeviation
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
