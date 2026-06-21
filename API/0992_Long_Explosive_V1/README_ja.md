# Long Explosive V1 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Long Explosive V1は、終値が前のバーに対して定義されたパーセンテージだけ上昇したときにロングポジションを建てます。設定されたパーセンテージだけ価格が下落したとき、または新しいロング取引を開く前にポジションをクローズします。

## 詳細

- **エントリー条件**:
  - **ロング**: `Close - PrevClose > Close * Price increase (%) / 100`.
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: `Close - PrevClose < -Close * Price decrease (%) / 100` または新しいロングエントリーの前。
- **ストップ**: なし。
- **デフォルト値**:
  - `Price increase (%)` = 1
  - `Price decrease (%)` = 1
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロング
  - インジケーター: 価格
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
