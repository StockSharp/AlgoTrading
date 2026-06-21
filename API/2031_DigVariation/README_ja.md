# DigVariation戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMQL5の*DigVariation*サンプルにインスパイアされています。単純移動平均（SMA）を使用してインジケーターを近似し、SMAの方向が変わるときに取引を開始します。

## ロジック
- SMAは入力されるローソク足で計算されます。
- 以前のSMA値が上昇傾向にあり、最新値がさらに高い場合、戦略はロングポジションを開きます。
- 以前のSMA値が下降傾向にあり、最新値がさらに低い場合、戦略はショートポジションを開きます。
- トレンドが反転すると既存のポジションは決済されます。

## パラメーター
- **Period** – SMAの計算期間。
- **BuyOpen** – ロングエントリーを有効にする。
- **SellOpen** – ショートエントリーを有効にする。
- **BuyClose** – ロングポジションの決済を許可する。
- **SellClose** – ショートポジションの決済を許可する。
- **StopLoss** – 損失保護値（`StartProtection`に渡される）。
- **TakeProfit** – 利益目標値（`StartProtection`に渡される）。

## 注意事項
これは簡略化された変換です。オリジナルのカスタムDigVariationインジケーターの代わりに標準SMAを使用しています。
