# Pivot Point SuperTrendトレンドフィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ピボットベースのSuperTrend線をSuperTrendトレンドフィルターおよび移動平均による確認と組み合わせます。トレンドが転換したとき、または指定した日付ウィンドウ内にピボットSuperTrendシグナルが現れたときに取引します。

## 詳細

- **エントリー条件**:
  - ロング：価格SuperTrendが上向きに転換し終値が終値SMAを上回る、またはピボットSuperTrendが日付範囲内で上向きに転換する。
  - ショート：価格SuperTrendが下向きに転換し終値がSMAを下回る、またはピボットSuperTrendが日付範囲内で下向きに転換する。
- **エグジット条件**:
  - いずれかの下向き転換でロングを全決済、上向き転換でショートを全決済する。SMAと日付は決済を制限しない。
  - 反対方向のエントリー条件が成立した場合のみ反転する。同時に逆方向の転換が発生したら決済だけ行う。
- **ストップ**: なし
- **デフォルト値**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
  - `CandleType` = 5分
  - `StartDate` / `EndDate` = UTC `DateTime.MinValue` / `DateTime.MaxValue`
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Pivot, SuperTrend, SMA
  - ストップ: いいえ
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: オプション
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

## 計算と執行

確定足だけを使用する。ピボット高値・安値は左右各 `PivotPeriod` 本の高値・安値より厳密に高い・低い必要があり、右側の足がすべて確定した時点で確認する。同じ足が両方の条件を満たす場合は高値を優先する。最初のピボットで `center` を初期化し、以降は `(2 * center + pivot) / 3` で更新する。

ピボットバンドはWilder ATRを使用する。最初の `AtrPeriod` 個のtrue rangeを平均し、以降は重み `1 / AtrPeriod` で平滑化する。基本支持・抵抗は `center ± Factor * ATR`。前の終値が前の支持を上回る場合、支持は基本値と前値の最大値を保持し、それ以外は再設定する。前の終値が前の抵抗を下回る場合、抵抗は最小値を保持し、それ以外は再設定する。終値が前の抵抗を厳密に上回れば上向き、前の支持を厳密に下回れば下向き、それ以外は方向を維持する。初期方向は上向きで、初期化はシグナルではない。

独立した価格フィルターはStockSharpの標準SuperTrend（`TrendAtrPeriod`、`TrendMultiplier`）を使用する。SMAは現在の終値を含む。シグナルは方向転換だけであり、両ATR、SMA、ピボット中心の準備が必要。`StartDate <= candle.OpenTime <= EndDate` はピボットの新規エントリーだけに適用する。日付は順序の正しいUTC値とする。新規数量は `Volume`、反転注文は `Volume + abs(Position)`、決済は `abs(Position)`。待機時間や保護ストップはない。

ピボットの概念：[LonesomeTheBlueのPivot Point Supertrend](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/)。上記が本例の双方向モデルの定義となる。
