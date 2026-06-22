# Color Laguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Color Laguerreオシレーターに基づくトレンドフォロー戦略。

Color Laguerreオシレーターはラゲールフィルターを使用して価格系列を平滑化し、色の変化によってトレンド方向を示します。オシレーターが強気に転じると買い、弱気に転じると売ります。価格モメンタムが弱まると極値レベルでの強制決済が発生することがあります。

## 詳細

- **エントリー条件**: オシレーターが中央レベルを越えるとき。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナルまたはストップ。
- **ストップ**: あり。
- **デフォルト値**:
  - `Gamma` = 0.7m
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: オシレーター
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1h)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

