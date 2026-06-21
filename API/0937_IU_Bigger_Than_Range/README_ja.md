# IU レンジ超え戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ローソク足の実体が直近のローソク足の前回レンジより大きいときにトレードを開くブレイクアウト戦略。

このシステムは現在のローソク足の実体を、設定可能なルックバック期間内の最高・最低の始値/終値間のレンジと比較します。実体が前回のレンジを超えた場合、ローソク足の方向にエントリーし、設定可能なストップ手法でリスクを管理します。

## 詳細

- **エントリー条件**: ローソク足の実体が前回レンジより大きい；方向はローソク足の実体による。
- **ロング/ショート**: 両方。
- **エグジット条件**: ストップロスまたはテイクプロフィット。
- **ストップ**: 前のローソク足、ATR、またはスイングレベル。
- **デフォルト値**:
  - `LookbackPeriod` = 22
  - `RiskToReward` = 3
  - `StopLossMethod` = PreviousHighLow
  - `AtrLength` = 14
  - `AtrFactor` = 2m
  - `SwingLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Highest, Lowest, ATR
  - ストップ: はい
  - 複雑さ: 中
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
