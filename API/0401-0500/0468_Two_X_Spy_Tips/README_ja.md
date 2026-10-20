# Two X Spy Tips

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

SpySecurity と TipsSecurity は Universe 内の異なる明示的なシグナルで、取引対象 Security も含めます。毎月最初の同期確定足で、両方の最新確定日終値が自身を含む SmaLength 日 SMA を厳密に上回る必要があります。形成済みの不成立条件でも当月審査を消費し、履歴不足だけが判断を遅らせます。空なら Capital×Leverage/価格を VolumeStep で切り捨てて買い、出口・売り建て・月次追加購入なしで保有します。USD、MinTradeUsd、銘柄固有数量制限を適用します。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | 100000 |
| `Leverage` | 2 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(1440).TimeFrame() |
| `PriceHistoryFile` | 未指定 |
| `SpySecurity` | 必須 |
| `TipsSecurity` | 必須 |
| `SmaLength` | 200 |

## 入力データ

任意の `PriceHistoryFile` CSV：`SecurityId,Date,Close`。終値は正、銘柄・日付は一意、日付は `yyyy-MM-dd`。日中判断では当日 UTC 日付より前だけを使い、確定日足は自身の終値を使えます。将来行で先取りせず、取引足が該当日の値を更新します。

## 検証

両言語を同梱 BTC/TON、独立式、注文方向・数量、実約定、保有、出口で検証します。日付付き価格・評価・暦データは機構検証で、OHLC・出来高の日付移動は複数年の市場履歴ではありません。2区分、TopN=1、SMA=2 は明示的な小規模テスト設定です。長期窓と元の初期値を別に確認し、収益は保証しません。
