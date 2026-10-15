# 勢力均衡戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

勢力均衡戦略は、終値と取引レンジを比較することで各ローソク足内の強気と弱気の力を評価します。この値が正の閾値を上抜けたとき、強い買い圧力を示します。

Balance of Power が定義された `Threshold` を上回ったときにロングポジションを建て、負の閾値を下回ったときに決済します。

## 詳細

- **エントリー条件**:
  - Balance of Power が `Threshold` を上抜け。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - Balance of Power が `-Threshold` を下抜け。
- **ストップ**: なし。
- **デフォルト値**:
  - `Threshold` = 0.8
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロング
  - インジケーター: Balance of Power
  - ストップ: なし
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
