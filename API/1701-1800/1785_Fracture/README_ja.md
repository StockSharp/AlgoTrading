# Fracture 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Fractureは、フラクタルのブレイクアウトと平滑移動平均、ADXを組み合わせて、レンジ相場とトレンド相場の両方で取引します。

## 詳細

- **エントリー条件**: ADXが閾値を下回る場合、価格が高速SMMaの上/下にある場合、最後の上昇フラクタルの上でロング、または最後の下降フラクタルの下でショートに入る。トレンドレジーム（高速SMMaが遅い線より上/下）では、価格が高速SMMaを交差したときにトレンド方向にエントリーする。
- **ロング/ショート**: 両方。
- **エグジット条件**: 利益がATRと`MinProfit`の積を超えたらポジションを閉じる。
- **ストップ**: ATRベースの利益目標。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `AtrPeriod` = 14
  - `AdxPeriod` = 22
  - `AdxLine` = 40
  - `Ma1Period` = 5
  - `Ma2Period` = 9
  - `Ma3Period` = 22
  - `RangingMultiplier` = 0.5
  - `MinProfit` = 1
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: ロング & ショート
  - インジケーター: Fractal, SMMA, ATR, ADX
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
