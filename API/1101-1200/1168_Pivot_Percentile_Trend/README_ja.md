# Pivot Percentile Trend戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は複数のルックバック期間にわたるパーセンタイルベースのトレンド強度を計算し、SuperTrendフィルターと組み合わせます。トレンド強度が正でかつ価格がSuperTrend線の上にある場合にロング取引を行います。トレンド強度が負でかつ価格がSuperTrendの下にある場合にショート取引を行います。

## 詳細

- **インジケーター**: パーセンタイル分析、SuperTrend
- **方向**: 両方
- **時間軸**: 任意

## このサンプルの計算方法

7つの窓 `PercentileLength * 1..7` で High/Low の四分位数（75%/25%、nearest rank）を計算します。現在の High が上側四分位数を厳密に上回る場合は +1、Low が下側四分位数を厳密に下回る場合は -1 とします。現在の足も各窓に含めます。

合計がトレンド強度です。強度が正で Close が価格 SuperTrend より上ならロング、負で Close が下ならショートです。反対のポジションは完全に反転して `Volume` にします。中立またはフィルター不成立ではポジションを維持します。全窓の形成を待ち、シグナルの待機時間や保護ストップは設けません。

初期値：`PercentileLength=40`、`AtrPeriod=10`、`AtrMultiplier=3`、`CandleType=5m`。SuperTrend はプラットフォームの Wilder ATR と価格バンドを使います。
