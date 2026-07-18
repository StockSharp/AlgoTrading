# Sunil BB ブラスト Heikin Ashi 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bollinger Bandsのブレイクアウトと Heikin Ashi ローソク足の確認を組み合わせます。

この戦略は、直前の Heikin Ashi および標準ローソク足の方向に沿ったBollinger Bandsのブレイクアウトを待ちます。ポジションは反対側のバンドをストップとして使用し、リスク・リワード比に基づいたターゲットを設定します。

## 詳細

- **エントリー条件**: 直前の Heikin Ashi とローソク足が同方向でBollinger Bandsをブレイク。
- **ロング/ショート**: `Direction` で設定可能。
- **エグジット条件**: バンドに基づく利益確定またはストップロス。
- **ストップ**: Bollinger Bandとリスク/リワード比。
- **デフォルト値**:
  - `BollingerPeriod` = 19
  - `BollingerMultiplier` = 2m
  - `RiskRewardRatio` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Direction` = TradeDirection.Both
  - `SessionBegin` = 09:20:00
  - `SessionEnd` = 15:00:00
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Bollinger, HeikinAshi
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
