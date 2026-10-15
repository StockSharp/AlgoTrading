# Super Woodies CCI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はオリジナルのMQL5エキスパートアドバイザー*Exp_SuperWoodiesCCI*をコンバートしたものです。上位時間軸で計算された商品チャネル指数（CCI）の方向に基づいて取引します。

## ロジック

- 設定可能な期間でCCIを計算します。
- CCIがゼロを上抜けたとき：
  - オプションでショートポジションを決済します。
  - オプションでロングポジションを建てます。
- CCIがゼロを下抜けたとき：
  - オプションでロングポジションを決済します。
  - オプションでショートポジションを建てます。

確定したローソク足のみが処理され、戦略は指定されたローソク足タイプで動作します。

## パラメーター

- **CciPeriod** – CCI計算の期間。
- **CandleType** – 分析するローソク足の時間軸。
- **AllowLongEntry** – ロングポジションの建玉を有効化。
- **AllowShortEntry** – ショートポジションの建玉を有効化。
- **AllowLongExit** – CCIが負のときにロングポジションの決済を有効化。
- **AllowShortExit** – CCIが正のときにショートポジションの決済を有効化。

## 注意事項

この戦略はStockSharpの高レベルAPIを`SubscribeCandles`とインジケーターバインディングで使用します。ポジション管理には`BuyMarket`と`SellMarket`の取引メソッドが使用されます。
