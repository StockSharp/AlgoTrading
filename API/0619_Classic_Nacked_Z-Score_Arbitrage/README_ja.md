# クラシック・ネイキッドZ-Score裁定戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はZ-Scoreを使用して2つの資産間のスプレッドを取引します。スプレッドのz-scoreがプラスの閾値を上回ると、戦略は第1の資産を売り、第2の資産を買います。z-scoreがマイナスの閾値を下回ると、第1の資産を買い、第2の資産を売ります。z-scoreがゼロに向かって戻るとポジションを決済します。

## パラメーター
- ローソク足タイプ
- ルックバック期間
- Z-Score閾値
- 第2の銘柄
