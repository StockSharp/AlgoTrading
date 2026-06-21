# CoeffofLine True 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は MQL5 エキスパート `Exp_CoeffofLine_true.mq5` を StockSharp フレームワークに移植したものです。中値価格の**線形回帰スロープ**を追跡し、ゼロクロスに反応します。

スロープが負の値から正になるとロングポジションを開きます。スロープが正の値から負になるとショートポジションを開きます。既存のポジションは反対のシグナルで決済されます。完成したローソク足のみが処理されます。

## パラメーター

- **Candle Type** – ローソク足シリーズの時間軸。
- **Slope Period** – スロープ計算に使用する線形回帰の長さ。
- **Signal Bar** – シグナル評価に使用する過去バーのインデックス。
- **Buy Open / Sell Open** – ロングまたはショートポジションを開く権限。
- **Buy Close / Sell Close** – ロングまたはショートポジションを閉じる権限。

戦略はローソク足を購読し、高レベル API を通じてインジケーターをバインドし、インジケーター値の手動取得なしに動作します。
