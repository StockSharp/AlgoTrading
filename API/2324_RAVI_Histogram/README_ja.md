# RAVIヒストグラム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderのRAVI HistogramエキスパートをStockSharpに変換したものです。高速EMAと低速EMAの差をパーセンテージで計測してトレンドの強さを測定します。結果は上位レベルと下位レベルと比較され、売買タイミングを決定します。

RAVI値が上位レベルを上回ると、市場は強気と判断されます。ショートポジションがクローズされ、有効な場合はロングポジションがオープンされます。値が下位レベルを下回ると、戦略はロングをクローズし、ショートをオープンすることがあります。デフォルトでは4時間足ローソク足で動作します。

## 詳細

- **エントリー条件**:
  - **ロング**: RAVIが`UpLevel`を上向きに突破。
  - **ショート**: RAVIが`DownLevel`を下向きに突破。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 反対のRAVIシグナルが既存ポジションをクローズ。
- **ストップ**: なし。
- **フィルター**: なし。
- **時間軸**: デフォルトで4時間足ローソク足。
- **パラメーター**:
  - `FastLength` および `SlowLength` – RAVI計算用のEMA期間。
  - `UpLevel` および `DownLevel` – トレンドゾーンを定義する閾値。
  - `BuyOpen`、`SellOpen`、`BuyClose`、`SellClose` – 各方向の操作を有効/無効化。
