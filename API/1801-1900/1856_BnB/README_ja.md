# BnB 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTrader 5のエキスパートアドバイザー「Exp_BnB」のポートです。各ローソク足内の強気と弱気の圧力を測定し、指数移動平均で平滑化するカスタムBnB（Bulls and Bears）インジケーターを使用します。

## 動作方法

1. 完成した各ローソク足について、戦略はbullsとbearsの値を計算します。
2. 両系列はEMAで平滑化されます。
3. bulls線がbears線を上抜けると：
   - 既存のショートポジションを閉じます。
   - ロングポジションを開きます。
4. bears線がbulls線を上抜けると：
   - 既存のロングポジションを閉じます。
   - ショートポジションを開きます。
5. ストップロスとテイクプロフィットのレベルは絶対価格ポイントで管理されます。

## パラメーター

- `Candle Type` – 計算に使用するローソク足の時間軸。
- `EMA Length` – bullsとbearsの平滑化期間。
- `Stop Loss` – 価格ポイントでの保護ストップまでの距離。
- `Take Profit` – 価格ポイントでの利益目標までの距離。
- `Allow Long Entry` – ロングポジションの開設を有効にする。
- `Allow Short Entry` – ショートポジションの開設を有効にする。
- `Allow Long Exit` – ロングポジションの決済を有効にする。
- `Allow Short Exit` – ショートポジションの決済を有効にする。

## 注意事項

元のインジケーターは複数の平滑化方法をサポートしています。このポートでは汎用フィルターを標準的な指数移動平均で近似しています。
