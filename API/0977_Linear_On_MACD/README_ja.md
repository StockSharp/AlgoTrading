# MACD上の線形戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格と出来高の MACD シグナルを線形回帰と組み合わせた戦略。

## 詳細

- **エントリー条件**: 両方の MACD がシグナルを上回り、回帰価格が始値と終値の間にあるときロング。逆の条件でショート。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のシグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Lookback` = 21
  - `RiskHigh` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD, Linear Regression
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 変動
