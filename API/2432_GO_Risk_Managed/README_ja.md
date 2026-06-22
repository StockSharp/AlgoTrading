# GO リスク管理戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、オリジナルのMetaTraderスクリプト「GO」をC#に移植したものです。始値・高値・安値・終値の移動平均からカスタムオシレーターを計算し、市場の方向性を判断します。

## 戦略ロジック

1. Open、High、Low、Closeの各シリーズに対して、同じ期間と手法で4本の移動平均を構築します。
2. 完成した各ローソク足で*GO*値を計算します:
   
   `GO = ((MA_close - MA_open) + (MA_high - MA_open) + (MA_low - MA_open) + (MA_close - MA_low) + (MA_close - MA_high)) * Volume`
3. GO値が正になると、すべてのショートポジションを決済し、新しいロングポジションを建てます。
4. GO値が負になると、すべてのロングポジションを決済し、新しいショートポジションを建てます。
5. 1バーにつき1トレードのみ許可されます。オープンポジションの合計数が**Max Positions**に達するまで新規エントリーを行います。

## パラメーター

- **Risk %** – 取引量を計算するために使用するアカウント資産の割合。
- **Max Positions** – 一方向で許可されるオープンポジションの最大数。
- **MA Type** – 移動平均の種類（SMA、EMA、DEMA、TEMA、WMA、VWMA）。
- **MA Period** – すべての移動平均の期間。
- **Candle Type** – インジケーター計算に使用するローソク足シリーズ。

## 注記

この実装はStockSharpの高レベルAPIを使用しています。ローソク足を購読し、インジケーターをバインドしてチャートに描画します。取引量は指定されたリスク割合と銘柄の出来高制限に応じて調整されます。
