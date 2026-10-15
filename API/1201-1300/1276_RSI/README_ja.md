# RSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

相対力指数（RSI）に基づくシンプルな戦略です。RSIが売られすぎレベルを上抜けたときに買い、買われすぎレベルを下抜けたときに売ります。

## 詳細

- **エントリー条件**:
  - ロング: RSIが`OverSold`を上抜け
  - ショート: RSIが`OverBought`を下抜け
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 逆方向のシグナル
- **ストップ**: なし
- **デフォルト値**:
  - `RsiLength` = 14
  - `OverSold` = 25m
  - `OverBought` = 75m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
