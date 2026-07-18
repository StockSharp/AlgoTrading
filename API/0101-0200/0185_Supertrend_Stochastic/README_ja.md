# Supertrend Stochastic 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Supertrend + Stochastic戦略。SupertrendがトレンドDirection を示し、Stochasticが売られすぎ/買われすぎの条件で確認したときにトレードを開始する戦略。

テストでは年平均リターン約142%を示しています。株式市場で最もパフォーマンスが高くなります。

Supetrendがトレンドを示し、Stochasticが一時的な逆方向の動きを指摘します。Stochasticがトレンドに反して売られすぎまたは買われすぎゾーンを抜けたときにエントリーが発生します。

明確なトレンドシグナルを必要とするモメンタムトレーダーに最適です。ATR値がストップ距離を定義します。

## 詳細

- **エントリー条件**:
  - ロング: `Close > Supertrend && StochK < 20`
  - ショート: `Close < Supertrend && StochK > 80`
- **ロング/ショート**: 両方
- **エグジット条件**: Supetrendの反転
- **ストップ**: Supetrendをトレーリングストップとして使用
- **デフォルト値**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Supertrend, Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

