# SuperTrend AI オシレーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SuperTrend AI OscillatorはSuperTrendのトレーリングストップとカスタムオシレーターフィルターを組み合わせます。
この戦略はオシレーターで確認されたSuperTrendの反転でトレードします。
ポジションはトレーリングストップとオプションのリスクリワード目標で管理されます。

## 詳細

- **エントリー条件**: SuperTrendの転換でオシレーター > 50（ロング）または < 50（ショート）
- **ロング/ショート**: 両方
- **エグジット条件**: トレーリングストップまたはリスクリワードのテイクプロフィット
- **ストップ**: トレーリング
- **デフォルト値**:
  - `AtrLength` = 10
  - `Factor` = 1
  - `RiskReward` = 2
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR, Stochastic
  - ストップ: トレーリング
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
