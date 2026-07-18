# DVDレベル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はオリジナルの「DVD Level」MQL5エキスパートアドバイザーの簡略化された翻訳です。市場の方向を判断するためにRange Action Verification Index（RAVI）を使用します。RAVIは1時間ローソク足に対する2期間と24期間の指数移動平均を使用して計算されます。

## パラメーター
- `Volume` – 取引に使用する注文ボリューム。

## ロジック
1. 1時間ローソク足を購読し、EMA(2)とEMA(24)を計算します。
2. `RAVI = (EMA2 - EMA24) / EMA24 * 100`を計算します。
3. RAVIがゼロを下回るとクロスした場合、フラットまたはショートであれば戦略は買います。
4. RAVIがゼロを上回るとクロスした場合、フラットまたはロングであれば戦略は売ります。
5. `StartProtection()`を介して組み込みのポジション保護が有効化されます。

このアプローチは、短期モメンタムが長期トレンドから乖離した際の潜在的な反転を捉えます。
