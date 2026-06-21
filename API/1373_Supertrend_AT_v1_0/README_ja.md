# Supertrend AT v1.0 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Supetrendベースの戦略で、Supetrendが下向きから上向きに転換したときにロングポジションを開き、上向きから下向きに転換したときにショートポジションを開きます。ポジションサイズはトレードあたりのリスクから計算され、エグジットは前回のSupetrendから導いたストップロスとテイクプロフィットを使用します。

## 詳細

- **エントリー条件**: Supetrendの方向転換。
- **ロング/ショート**: ロングとショート。
- **エグジット条件**: 目標またはストップに到達。
- **ストップ**: はい。
- **デフォルト値**:
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3m
  - `RiskPerTrade` = 2m
  - `RewardRatio` = 3m
  - `CommissionPercent` = 0.05m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング & ショート
  - インジケーター: Supertrend
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
