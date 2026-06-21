# Martin戦略 - 損失なし決済 V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このマーチンゲール平均化戦略は、最初のエントリーから設定された割合だけ価格が下落するたびにロングポジションに追加します。新しい注文ごとに現金額が乗数で増加し、平均価格が調整されます。ポジションはローソク足の高値が平均価格にテイクプロフィット率を加えた値に達したときに決済され、利益がある場合のみの出口が保証されます。

## 詳細

- **エントリー条件**:
  - **ロング**: `フラット` → `Initial Cash`で購入
  - **追加**: `Price <= EntryPrice * (1 - PriceStep% * orderCount)` && `orderCount < MaxOrders`
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - `High >= AvgPrice * (1 + TakeProfit%)`
- **ストップ**: いいえ
- **デフォルト値**:
  - `Initial Cash` = 100
  - `Max Orders` = 20
  - `Price Step %` = 1.5
  - `Take Profit %` = 1
  - `Increase Factor` = 1.05
- **フィルター**:
  - カテゴリ: ナンピン買い
  - 方向: ロングのみ
  - インジケーター: なし
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
