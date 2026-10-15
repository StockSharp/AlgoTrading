# Pivot Point Supertrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Pivot PointsとATRベースのSupertrendを組み合わせてトレンドの転換を捉える戦略です。

Pivot Pointsが動的な中心線を定義します。ATR乗数が価格に追従する上下バンドを形成します。トレンドが方向を転換すると、戦略はそれに応じてエントリーします。

## 詳細

- **エントリー条件**: Pivot PointsとATR Supertrendに基づくシグナル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Pivot Points, ATR
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

## 計算と執行

確定足だけを使用する。ピボット高値・安値は左右各 `PivotPeriod` 本の高値・安値より厳密に高い・低い必要があり、右側の足がすべて確定した時点で確認する。同じ足が両方の条件を満たす場合は高値を優先する。最初のピボットで `center` を初期化し、以降は `(2 * center + pivot) / 3` で更新する。

ピボットバンドはWilder ATRを使用する。最初の `AtrPeriod` 個のtrue rangeを平均し、以降は重み `1 / AtrPeriod` で平滑化する。基本支持・抵抗は `center ± AtrFactor * ATR`。前の終値が前の支持を上回る場合、支持は基本値と前値の最大値を保持し、それ以外は再設定する。前の終値が前の抵抗を下回る場合、抵抗は最小値を保持し、それ以外は再設定する。終値が前の抵抗を厳密に上回れば上向き、前の支持を厳密に下回れば下向き、それ以外は方向を維持する。初期方向は上向きで、初期化はシグナルではない。

Wilder ATRと確認済みピボット中心の準備が必要。ピボットバンドの方向転換だけでエントリーし、上向きでロング、下向きでショートとなる。初期方向は上向きで、初期化では発注しない。反対シグナルごとに数量 `Volume + abs(Position)` の1件の成行注文で全反転する。保有なしの新規注文は `Volume`。SMA、別の価格SuperTrend、日付フィルター、待機時間、保護ストップはない。

ピボットの概念：[LonesomeTheBlueのPivot Point Supertrend](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/)。上記が本例の双方向モデルの定義となる。
