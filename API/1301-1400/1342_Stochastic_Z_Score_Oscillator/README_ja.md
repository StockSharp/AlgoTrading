# Stochastic Z-Scoreオシレーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

スケーリングされたStochasticオシレーターと価格Z-Scoreを組み合わせます。それらの平均が閾値を超えたときに取引を開き、Z-Scoreがゼロに戻ったときに閉じます。クールダウンカウンターが頻繁なシグナルを防ぎます。

## 詳細

- **エントリー条件**: スケーリングされたStochastic %Kと価格Z-Scoreの平均がクールダウン後に閾値を上/下抜ける
- **ロング/ショート**: 両方
- **エグジット条件**: Z-Scoreがゼロを通過
- **ストップ**: なし
- **デフォルト値**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `StochLength` = 14
  - `StochSmooth` = 7
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Stochastic, SMA, StandardDeviation
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
