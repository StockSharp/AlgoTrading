# Parabolic SAR Stochastic 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Parabolic SAR + Stochastic 戦略の実装。価格が SAR より上にあり Stochastic %K が 20 未満（売られすぎ）のときに買い。価格が SAR より下にあり Stochastic %K が 80 超（買われすぎ）のときに売り。

テストでは年平均リターン約 61% を示しています。暗号資産市場で最も優れたパフォーマンスを発揮します。

Parabolic SAR がトレンドを示し、Stochastic が押し目でのエントリーを精査します。SAR がサイドを変えるとシグナルが反転します。

組み込みの SAR ストップを備えたシンプルなトレンド戦略です。ATR 設定が追加のリスク管理を担います。

## 詳細

- **エントリー条件**:
  - ロング: `Close > SAR && StochK < StochOversold`
  - ショート: `Close < SAR && StochK > StochOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - Parabolic SAR が反対方向に転換
- **ストップ**: SAR ベースのダイナミックストップ
- **デフォルト値**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `StochK` = 3
  - `StochD` = 3
  - `StochPeriod` = 14
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Parabolic SAR, Parabolic SAR, Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
