# Ilan14戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Ilan14は、ロングとショートのポジションを同時に開くヘッジグリッド戦略です。市場がユーザー定義のピップ距離分一方向に逆行すると、その方向に**Lot Exponent**でボリュームを乗じた新規注文を追加します。ポジションの平均価格が追跡され、価格が設定した**Take Profit**距離分戻ると、その側のすべての注文が決済されます。

パラメーター:
- **Pip Step** – グリッド注文間のピップ距離。
- **Lot Exponent** – 各追加注文のボリュームに適用する乗数。
- **Max Trades** – 方向ごとの最大注文数。
- **Take Profit** – 加重平均価格からのピップ単位の利益目標。
- **Initial Volume** – 最初の注文のボリューム。
- **Candle Type** – ローソク足サブスクリプションの時間軸。

この実装はローソク足サブスクリプションによるStockSharpの高レベルAPIを使用し、手動のデータコレクションを回避しています。グリッドの両側が独立して管理されるため、不利な動きの後の反発を捉えることができます。
