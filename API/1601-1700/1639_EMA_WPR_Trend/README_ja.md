# EMA WPRトレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAトレンドフィルターとWilliams %Rシグナルを組み合わせた戦略。売られすぎレベルで買い、買われすぎレベルで売ります。押し目しきい値によって連続したエントリーを防ぎます。オプションのエグジットは、Williams %Rの反対の極値で、または複数の不採算バー後にトレードを決済します。

## 詳細

- **エントリー条件**:
  - ロング: Williams %R <= -100 かつ EMAトレンドが上向き
  - ショート: Williams %R >= 0 かつ EMAトレンドが下向き
- **ロング/ショート**: 両方
- **エグジット条件**:
  - `UseWprExit`が有効な場合、Williams %Rが反対の極値を突破
  - `UseUnprofitExit`が有効な場合、ポジションが`MaxUnprofitBars`本のバーにわたって不採算
- **ストップ**: いいえ
- **デフォルト値**:
  - `WprPeriod` = 46
  - `WprRetracement` = 30
  - `EmaPeriod` = 144
  - `BarsInTrend` = 1
  - `MaxUnprofitBars` = 5
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: EMA, Williams %R
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
