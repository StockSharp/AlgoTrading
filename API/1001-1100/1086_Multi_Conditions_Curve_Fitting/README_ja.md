# 複数条件カーブフィッティング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAクロスオーバー、RSI、ストキャスティクスオシレーターを組み合わせ、複数のシグナルが揃ったときに取引します。

## 詳細

- **エントリー条件**:
  - ロング: `FastEMA > SlowEMA` かつ `RSI < RsiOversold` かつ `StochK < 20`
  - ショート: `FastEMA < SlowEMA` かつ `RSI > RsiOverbought` かつ `StochK > 80`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: `FastEMA < SlowEMA` または `RSI > RsiOverbought` または `StochK > StochD`
  - ショート: `FastEMA > SlowEMA` または `RSI < RsiOversold` または `StochK < StochD`
- **ストップ**: なし
- **デフォルト値**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 25
  - `RsiLength` = 14
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `StochLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA, RSI, Stochastic
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
