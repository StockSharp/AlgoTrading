# 時刻別相対出来高
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

特定の時刻における出来高を直近のローソク足の平均出来高と比較する戦略。

## 詳細

- **エントリー条件**: 指定された時刻に相対出来高がしきい値を上回る。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 相対出来高が1を下回る。
- **ストップ**: なし。
- **デフォルト値**:
  - `Period` = 5
  - `Threshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TargetHour` = 9
  - `TargetMinute` = 30
- **フィルター**:
  - カテゴリ: 出来高
  - 方向: 両方
  - インジケーター: SMA, Volume
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
