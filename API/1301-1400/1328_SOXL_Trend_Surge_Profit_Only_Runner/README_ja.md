# SOXL トレンドサージ利益専用戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格が200 EMAを上回ってトレンドし、SuperTrendが強気の場合にロングトレードへエントリーします。ATRの上昇、平均を上回る出来高、セッションフィルター、価格が小さなEMAバッファの外にあることが必要です。ATRベースの目標で部分利確を行い、残りのポジションをATRストップでトレーリングします。

## 詳細

- **エントリー条件**: 価格がEMAを上回り、SuperTrendが上向き、出来高が平均以上、ATRが上昇、EMAバッファの外、時刻が14〜19時、エグジット後のクールダウン
- **ロング/ショート**: ロングのみ
- **エグジット条件**: ATR目標で50%部分利確とその後のトレーリングストップ
- **ストップ**: トレーリング
- **デフォルト値**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultTarget` = 2.0
  - `CooldownBars` = 15
  - `SupertrendFactor` = 3.0
  - `SupertrendAtrPeriod` = 10
  - `MinBarsHeld` = 2
  - `VolFilterLen` = 20
  - `EmaBuffer` = 0.005
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: EMA, ATR, SuperTrend, 出来高
  - ストップ: トレーリング
  - 複雑さ: 中程度
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
