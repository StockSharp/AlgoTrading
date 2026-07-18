# 純粋プライスアクション戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

直近の高値・安値からBreak of Structure (BOS)とMarket Structure Shift (MSS)を検出するシンプルなプライスアクション戦略。

BOSでロングエントリー、MSSでショートエントリーし、固定パーセンテージのストップロスとテイクプロフィットを使用します。

## 詳細

- **エントリー条件**: ロングにはBOS、ショートにはMSS。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロスまたはテイクプロフィット。
- **ストップ**: 固定パーセンテージ。
- **デフォルト値**:
  - `Length` = 5
  - `SlPercent` = 1m
  - `TpPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: Highest, Lowest
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
