# ハンマー + EMA戦略（ティックベースSL/TP）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

HammerおよびInverted Hammerのローソク足パターンと、EMAトレンドフィルターおよびティックベースのリスク管理を組み合わせた戦略です。

## 詳細

- **エントリー条件**: EMAより上のHammer、またはEMAより下のInverted Hammer。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ティックベースのテイクプロフィットまたはストップロス。
- **ストップ**: ティックベース。
- **デフォルト値**:
  - `EmaLength` = 50
  - `StopLossTicks` = 1
  - `TakeProfitTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: EMA, Hammer, Inverted Hammer
  - ストップ: ティックベース
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
