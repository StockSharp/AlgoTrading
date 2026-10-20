# Ema50 Crossover Monthly Dca

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

確定週足と EmaLength 終値 EMA を使い、最初の完全な窓の平均で初期化します。StartDate 以降の終値が対象です。最初に観測した対象暦月に DcaAmount を一度計上し、その後は EMA 形成中も含め経過月分を計上します。初回観測前の何十年分もの拠出は生成しません。EMA より上では蓄積現金を数量丸めして投資し、下なら実買い残高を全売却し、等値なら保持します。現金台帳は実約定と総売却代金に追従する戦略配分で、口座入金や融資モデルではありません。未完了注文と数量制限を守り、残金を維持します。売り建て、RSI、待機、保護ストップはありません。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(10080).TimeFrame() |
| `EmaLength` | 50 |
| `DcaAmount` | 100000 |
| `StartDate` | 1980-01-01 |

## 検証

両言語を同梱 BTC/TON、独立式、注文方向・数量、実約定、保有、出口で検証します。日付付き価格・評価・暦データは機構検証で、OHLC・出来高の日付移動は複数年の市場履歴ではありません。2区分、TopN=1、SMA=2 は明示的な小規模テスト設定です。長期窓と元の初期値を別に確認し、収益は保証しません。
