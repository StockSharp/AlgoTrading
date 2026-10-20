# Williams Alligator ATR

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

3本の Alligator 線は中央値 (高値+安値)/2 を平滑化します。Length と Shift は平滑化期間と確定足の遅延です。Teeth も形成を待ちますが、追加の入場条件ではありません。全線と Wilder ATR 形成後、Lips−Jaw が <=0 から >0 へ交差すると空から Volume の買いを建て、>=0 から <0 なら実買い残高を全決済します。売り建てはしません。入場時の ATR×AtrMultiplier を実約定からのネイティブローカル成行ストップ距離に固定し、足間の Bid/Ask でも執行します。待機、利食い、後の ATR による移動はありません。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `JawLength` | 13 |
| `TeethLength` | 8 |
| `LipsLength` | 5 |
| `JawShift` | 8 |
| `TeethShift` | 5 |
| `LipsShift` | 3 |
| `AtrPeriod` | 14 |
| `AtrMultiplier` | 2 |

## 検証

両言語を同梱 BTC/TON、独立式、注文方向・数量、実約定、保有、出口で検証します。日付付き価格・評価・暦データは機構検証で、OHLC・出来高の日付移動は複数年の市場履歴ではありません。2区分、TopN=1、SMA=2 は明示的な小規模テスト設定です。長期窓と元の初期値を別に確認し、収益は保証しません。
