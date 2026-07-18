# AI 出来高戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

AI 出来高戦略は突然の参加急増を狙います。現在の出来高がその EMA を所定の乗数で上回ると出来高スパイクが発生します。スパイクが 50 期間の価格 EMA とローソク足の色に一致する場合、その方向にエントリーします。各取引は固定バー数後に決済されます。

## 詳細

- **エントリー条件**: 出来高 > VolumeEMA * VolumeMultiplier かつ価格が 50 EMA の上/下でローソク足の色が一致。
- **ロング/ショート**: 両方向。
- **エグジット条件**: `ExitBars` 本のローソク足後にポジション決済。
- **ストップ**: なし。
- **デフォルト値**:
  - `VolumeEmaLength` = 20
  - `VolumeMultiplier` = 2.0
  - `ExitBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: 出来高ブレイクアウト
  - 方向: 両方
  - インジケーター: EMA, Volume EMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
