# ZScoreによる売買戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はZScoreを使用して移動平均からの極端な乖離を検出します。
ZScoreが閾値を上下にクロスするとポジションが建てられ、クールダウンによって重複シグナルが防止されます。

## 詳細

- **エントリー条件**:
  - ZScore > `ZThreshold` かつ売りクールダウンが経過した場合にショート。
  - ZScore < -`ZThreshold` かつ買いクールダウンが経過した場合にロング。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: SMA, StandardDeviation, Z-Score
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
