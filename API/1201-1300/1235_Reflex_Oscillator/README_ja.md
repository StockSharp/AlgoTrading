# Reflex オシレーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はJohn EhlersのReflex Oscillatorを使用します。オシレーターが上限閾値を上抜けるとロングに入り、下限閾値を下抜けるとショートに入ります。オシレーターがゼロラインに戻るとポジションを決済します。

## 詳細

- **エントリー条件**:
  - **ロング**: オシレーターが`UpperLevel`を上抜ける。
  - **ショート**: オシレーターが`LowerLevel`を下抜ける。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - ロングポジション: オシレーターがゼロを下抜ける。
  - ショートポジション: オシレーターがゼロを上抜ける。
- **ストップ**: なし。
- **デフォルト値**:
  - `ReflexPeriod` = 20.
  - `SuperSmootherPeriod` = 8.
  - `PostSmoothPeriod` = 33.
  - `UpperLevel` = 1.
  - `LowerLevel` = -1.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: 単一
  - ストップ: いいえ
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
