# ポジションの段階的積み増し・段階的縮小戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、各バーで利用可能な資金の一定割合を投資することでポジションを段階的に積み上げます。ポジション価値が設定された利益水準に達すると、ポジションの一部を売却し、オプションで実現利益の一部を別に保管します。

## 詳細

- **エントリー条件**: 資金が利用可能な場合は常に買い。
- **エグジット条件**: 利益率が閾値を超えたら売り。
- **ロング/ショート**: ロングのみ。
- **デフォルト値**:
  - `Buy Scaling Size %` = 2
  - `Take Profit Level %` = 50
  - `Take Profit Size %` = 1
  - `Retain Profit Portion %` = 50
  - `Minimum Position Value` = 200000
  - `Minimum Buy Value` = 100
- **フィルター**:
  - カテゴリ: その他
  - 方向: ロング
  - インジケーター: なし
  - ストップ: いいえ
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
