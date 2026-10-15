# Lacust ストップと BE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、オリジナルのMQLエキスパートアドバイザー **lacuststopandbe** にインスパイアされた基本的なポジション管理を示します。

最後に完成したローソク足の方向にポジションをエントリーした後、戦略はいくつかの保護ルールを適用します：

- 初期ストップロスとテイクプロフィットは固定価格距離に設定されます。
- 利益が `BreakevenGain` に達すると、ストップがエントリー価格 + `Breakeven` に移動されます。
- 利益が `TrailingStart` を超えると、ストップが `TrailingStop` の距離で価格を追跡します。
- ストップレベルまたはテイクプロフィットレベルに触れるとポジションが決済されます。

パラメーター：

- `CandleType` – 処理に使用するローソク足シリーズ。
- `StopLoss` – 初期ストップロス距離。
- `TakeProfit` – 初期テイクプロフィット距離。
- `TrailingStart` – トレーリングストップを有効にするために必要な利益。
- `TrailingStop` – 現在価格からのトレーリングストップ距離。
- `BreakevenGain` – ストップをブレークイーブンに移動する前に必要な利益。
- `Breakeven` – ストップをブレークイーブンに移動した後に確定する利益。

このサンプルはStockSharpの高レベルAPIを使用しており、単純なMQL取引管理スクリプトを移植するためのテンプレートとして活用できます。
