# 4本WMA戦略（TP・SL付き）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

4本の移動平均のクロスオーバーを使用し、オプションのテイクプロフィット、ストップロス、代替エグジット条件を備えた戦略。

## 詳細

- **エントリー条件**:
  - ロング: Long MA1がLong MA2を上抜けるクロス
  - ショート: Short MA1がShort MA2を下抜けるクロス
- **ロング/ショート**: 設定可能
- **ストップ**: パーセンテージベースのTPとSL
- **デフォルト値**:
  - `LongMa1Length` = 10
  - `LongMa2Length` = 20
  - `ShortMa1Length` = 30
  - `ShortMa2Length` = 40
  - `MaType` = Wma
  - `EnableTpSl` = true
  - `TakeProfitPercent` = 1m
  - `StopLossPercent` = 1m
  - `Direction` = Both
  - `EnableAltExit` = false
  - `AltExitMaOption` = LongMa1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: 移動平均
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
