# VininI Trend戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 説明
この戦略はオリジナルのMQLエキスパートアドバイザー**Exp_VininI_Trend**をStockSharpに変換したものです。商品チャネル指数（CCI）を使ってVininI Trendオシレーターをエミュレートします。オシレーターが上位レベルを突破するか上向きに転換したときにロングポジションが開きます。オシレーターが下位レベルを下回るか下向きに転換したときにショートポジションが開きます。完成したローソク足のみで動作します。

## パラメーター
- **CCI Period** – CCIインジケーターの長さ。
- **Upper Level** – 買いシグナルのトリガーとなる閾値。
- **Lower Level** – 売りシグナルのトリガーとなる閾値。
- **Entry Modes** – `Breakdown`はレベルクロスに反応し、`Twist`は方向転換に反応します。
- **Candle Type** – 計算に使用するローソク足の時間軸。

## オリジナル
`MQL/1365/exp_vinini_trend.mq5`のMQL5戦略から変換されました。
