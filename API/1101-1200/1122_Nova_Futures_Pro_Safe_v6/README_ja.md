# Nova Futures PRO SAFE v6 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はトレンド、ボラティリティ、構造シグナルを組み合わせる。200 EMAとADXでトレンドを確認し、Bollinger BandsとKeltner Channelsのスクイーズブレイクアウトを検出し、Donchianレベルで高値・安値の構造ブレイクを判定する。オプションの上位時間軸フィルターとChoppiness Indexにより低品質な相場環境での取引を回避する。クールダウン期間によりポジション決済直後の再エントリーを防ぐ。

## 入力
- **EMA Length** — ベース指数移動平均の長さ
- **DMI Length** — ADXと方向性移動の期間
- **Min ADX** — トレンドとみなす最小ADX値
- **BB Length** — Bollinger Bands期間
- **BB Mult** — Bollinger Bandsの乗数
- **KC Length** — Keltner Channels期間
- **KC Mult** — Keltner Channelsの乗数
- **Donchian Length** — 構造レベルのルックバック
- **Use HTF** — 上位時間軸確認を有効化
- **HTF Candle** — フィルター用の上位時間軸
- **HTF EMA** — 上位時間軸のEMA長
- **HTF Min ADX** — 上位時間軸の最小ADX
- **Use Choppiness** — Choppinessフィルターを有効化
- **Chop Length** — Choppiness Indexの期間
- **Chop Threshold** — 許容される最大Choppiness値
- **Cooldown** — 決済後の待機バー数
- **Candle Type** — メインのローソク足時間軸

## 備考
TradingViewスクリプト「Nova Futures PRO (SAFE v6) — HTF + Choppiness + Cooldown」の簡易移植版。
