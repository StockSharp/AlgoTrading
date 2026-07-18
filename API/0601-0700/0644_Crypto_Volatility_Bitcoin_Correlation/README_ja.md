# 仮想通貨ボラティリティとBitcoin相関戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bitcoinのボラティリティが上昇してBVOL7Dインデックスとともに上がり、価格がEMAより高い場合にロングポジションに入る戦略。価格がEMAを下回るとエグジットする。

## 詳細

- **エントリー条件**: VIXFixが前の値より大きく、BVOL7Dが前の値より大きく、終値がEMAを上回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 終値がEMAを下回る。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `VixFixLength` = 22
  - `EmaLength` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: ロングのみ
  - インジケーター: Highest、EMA
  - ストップ: いいえ
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
