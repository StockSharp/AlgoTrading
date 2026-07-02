# Parabolic Sar Rsi 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
トレンドの方向にはParabolic SAR、売られすぎ/買われすぎ状態のエントリー確認にはRSIを組み合わせた戦略。

テストでは年平均リターン約166%を示しています。株式市場で最もパフォーマンスが高いです。

ここではParabolic SARが主要なトレンドを示し、RSIが枯渇度を測定します。両方のインジケーターが同じ方向にシグナルを出した時点でトレードが開かれます。

SARが動的な出口も提供するため、トレーリングストップが好きな人にとってこの組み合わせは魅力的です。ストップの配置はSARカーブに従います。

## 詳細

- **エントリー条件**:
  - ロング: `Close > SAR && RSI < RsiOversold`
  - ショート: `Close < SAR && RSI > RsiOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: `Close < SAR`
  - ショート: `Close > SAR`
- **ストップ**: Parabolic SARをトレーリングストップとして使用
- **デフォルト値**:
  - `SarAf` = 0.02m
  - `SarMaxAf` = 0.2m
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Parabolic SAR, Parabolic SAR, RSI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

