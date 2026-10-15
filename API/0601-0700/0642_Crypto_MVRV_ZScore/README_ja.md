# Crypto MVRV ZScore戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMVRV Z-Scoreのコンセプトを用いて市場価値と実現価値の間の極端な乖離を検出する。
スプレッドのz-scoreが事前に設定されたしきい値を超えるとポジションを開き、逆方向のクロスで決済する。

## 詳細

- **エントリー条件**:
  - z-scoreが`LongEntryThreshold`を上抜けした場合にロング。
  - z-scoreが`ShortEntryThreshold`を下抜けした場合にショート。
- **ロング/ショート**: 設定可能 (`TradeDirection`)。
- **エグジット条件**:
  - 逆方向のしきい値クロス。
- **ストップ**: なし。
- **デフォルト値**:
  - `ZScoreCalculationPeriod` = 252
  - `LongEntryThreshold` = 0.382
  - `ShortEntryThreshold` = -0.382
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: SMA、StandardDeviation、Z-Score
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
