# Heatmap MACD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このシステムは5つの時間軸のMACDヒストグラムのヒートマップを使用します。全てのヒストグラムがゼロより上または下に転じると対応する方向にエントリーし、アラインメントが崩れるかリスク限度が発動するとエグジットします。

## 詳細

- **エントリー条件**: 全てのMACDヒストグラムがゼロより上/下。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ヒストグラムのアラインメントが崩れるかストップが発動。
- **ストップ**: あり。
- **デフォルト値**:
  - `FastPeriod` = 20
  - `SlowPeriod` = 50
  - `SignalPeriod` = 50
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `CandleType1` = TimeSpan.FromMinutes(60)
  - `CandleType2` = TimeSpan.FromMinutes(120)
  - `CandleType3` = TimeSpan.FromMinutes(240)
  - `CandleType4` = TimeSpan.FromMinutes(240)
  - `CandleType5` = TimeSpan.FromMinutes(480)
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: マルチ時間軸
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
