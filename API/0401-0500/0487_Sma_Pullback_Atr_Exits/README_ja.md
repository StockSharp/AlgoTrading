# Sma Pullback Atr Exits

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

確定足で終値 SMA 2本と Wilder ATR が形成済みの場合、終値 < 高速 SMA かつ高速 > 低速なら買い、終値 > 高速かつ高速 < 低速なら売ります。等値は入場しません。空の状態から Volume を建て、未完了注文を待ちます。シグナル時に SL = ATR×AtrMultiplierSl、TP = ATR×AtrMultiplierTp を固定します。ネイティブのローカル成行保護が実約定に距離を固定し Bid/Ask を監視します。後の ATR では変更しません。保護が全残高を決済し、反対シグナルによる保有中の反転はありません。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `FastSmaLength` | 8 |
| `SlowSmaLength` | 30 |
| `AtrLength` | 14 |
| `AtrMultiplierSl` | 1.2 |
| `AtrMultiplierTp` | 2 |

## 検証

両言語を同梱 BTC/TON、独立式、注文方向・数量、実約定、保有、出口で検証します。日付付き価格・評価・暦データは機構検証で、OHLC・出来高の日付移動は複数年の市場履歴ではありません。2区分、TopN=1、SMA=2 は明示的な小規模テスト設定です。長期窓と元の初期値を別に確認し、収益は保証しません。
