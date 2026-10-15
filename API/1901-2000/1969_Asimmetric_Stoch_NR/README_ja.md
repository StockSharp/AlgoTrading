# Asimmetric Stoch NR 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

非対称ストキャスティクスオシレーターラインに基づく戦略です。%Kと%Dのクロスオーバーに反応し、オプションのポジション保護をサポートします。

このメソッドは市場ノイズに適応するために%K計算の期間を切り替えます。ストップロスとテイクプロフィットは絶対価格単位で適用されます。

## 詳細

- **エントリー条件**:
  - ロング: `%K` が `%D` を上向きにクロス
  - ショート: `%K` が `%D` を下向きにクロス
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: `%K` が `%D` を下向きにクロス
  - ショート: `%K` が `%D` を上向きにクロス
- **ストップ**: `StopLoss` と `TakeProfit` の絶対値
- **デフォルト値**:
  - `KPeriodShort` = 5
  - `KPeriodLong` = 12
  - `DPeriod` = 7
  - `Slowing` = 3
  - `Overbought` = 80m
  - `Oversold` = 20m
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 長期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
