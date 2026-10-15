# シンプル・マルチタイムフレーム移動平均戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は `simple_multiple_time_frame_moving_average.mq4` のロジックを再現します。2つの時間軸にわたってシンプル移動平均を使用することでトレンドを一致させます。

## 戦略ロジック
- 1時間足と4時間足のローソク足に対して期間 `Length` のSMAを計算します。
- 両方のSMAが上昇しているときにロングエントリーします。
- 両方のSMAが下落しているときにショートエントリーします。
- どちらかのSMAが下向きに転換した場合、ロングポジションをクローズします。
- どちらかのSMAが上向きに転換した場合、ショートポジションをクローズします。
- 同時にアクティブにできるポジションは1つだけです。

## パラメーター
- **MA Length** (`Length`): 両方の移動平均に使用する期間。
- **Short Time Frame** (`ShortCandleType`): 最初のSMAの時間軸（デフォルト: 1時間）。
- **Long Time Frame** (`LongCandleType`): 2番目のSMAの時間軸（デフォルト: 4時間）。

取引数量は戦略の `Volume` プロパティから取得します。

## 注意事項
この実装は、オリジナルのMQLバージョンで使用されている1時間足と4時間足の平均に焦点を当てており、使用されていない上位時間軸の計算は省略しています。
