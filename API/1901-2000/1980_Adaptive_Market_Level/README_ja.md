# 適応型市場水準
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Adaptive Market Level（AML）インジケーターに基づいて取引する戦略です。インジケーターは現在のボラティリティに適応し、動的な価格水準を描画します。AMLラインが上向きに転換するとロングポジションを開き、下向きに転換するとショートポジションを開きます。反対のポジションは色変化時またはストップロス/テイクプロフィット発動時にクローズされます。

このシステムは中期トレンドに従い、デフォルトでは高い時間軸で動作します。

## 詳細

- **エントリー条件**: AMLラインがロングは上向き、ショートは下向きに方向転換。
- **ロング/ショート**: 両方向。
- **エグジット条件**: AMLの方向転換またはストップ/目標。
- **ストップ**: はい。
- **デフォルト値**:
  - `Fractal` = 6
  - `Lag` = 7
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Adaptive Market Level
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: H4
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
