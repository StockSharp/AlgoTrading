# Simple MA ADX EA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAとADX（平均方向性指数）を組み合わせてトレンドの強さを確認する戦略。

EMAが上昇中で、前回の終値がEMAを上回り、ADXが閾値を超え、+DIが-DIより大きい場合に買います。逆の条件が現れると売ります。ストップロスとテイクプロフィットのレベルでリスクを管理します。

## 詳細

- **エントリー条件**: EMAの方向、価格とEMAの比較、ADX、+DI/-DI。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナルまたは保護注文。
- **ストップ**: はい。
- **デフォルト値**:
  - `AdxPeriod` = 8
  - `MaPeriod` = 8
  - `AdxThreshold` = 22m
  - `StopLoss` = 30m
  - `TakeProfit` = 100m
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, ADX
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
