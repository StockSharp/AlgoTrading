# マルチ時間軸 RSI 売買戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は3つの異なる時間軸のRSI値を使用します。有効なすべてのRSIが買いしきい値を下回ると、ロングポジションが建てられます。有効なすべてのRSIが売りしきい値を上回ると、ショートポジションが建てられます。クールダウン期間により、連続したシグナルが防止されます。

## 詳細

- **エントリー条件**: 有効なすべてのRSIがしきい値を下回る/上回る。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `Rsi1Length` = 14
  - `Rsi2Length` = 14
  - `Rsi3Length` = 14
  - `Rsi1CandleType` = TimeSpan.FromMinutes(5)
  - `Rsi2CandleType` = TimeSpan.FromMinutes(15)
  - `Rsi3CandleType` = TimeSpan.FromMinutes(30)
  - `BuyThreshold` = 30m
  - `SellThreshold` = 70m
  - `CooldownPeriod` = 5
- **フィルター**:
  - カテゴリ: Momentum
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: マルチ時間軸
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
