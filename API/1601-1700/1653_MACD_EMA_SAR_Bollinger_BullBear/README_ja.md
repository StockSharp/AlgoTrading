# MACD EMA SAR Bollinger BullBear戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACD、EMAクロス、Parabolic SAR、ボリンジャーバンド、Bulls/Bears Powerインジケーターを組み合わせます。アクティブな時間帯のみ取引します。

## 詳細

- **エントリー条件**:
  - **ロング**: MACD < Signal、直近2つの高値がボリンジャーバンド上限を下回る、EMA3 > EMA34、SARが価格を下回る、Bulls Power > 0 かつ減少中。
  - **ショート**: MACD > Signal、EMA3 < EMA34、SARが価格を上回る、Bears Power < 0 かつ増加中。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 専用の決済ルールなし。反対のシグナルでポジションをクローズ。
- **ストップ**: なし。
- **デフォルト値**:
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Fast EMA Period` = 3
  - `Slow EMA Period` = 34
  - `Power Period` = 13
  - `SAR Step` = 0.02
  - `SAR Max` = 0.2
  - `Bollinger Period` = 20
  - `Bollinger Deviation` = 2.0
  - `Candle Type` = 15分
  - `Session Start` = 09:00
  - `Session End` = 17:00
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
