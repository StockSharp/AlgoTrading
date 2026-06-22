# EMAクロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は2つの指数移動平均（EMA）のクロスオーバーを取引します。
ファストEMAがスローEMAを上回ったときにロングポジションがオープンされ、ファストEMAがスローEMAを下回ったときにショートポジションがオープンされます。
**Reverse**パラメーターはEMAの役割を入れ替え、エントリーシグナルを効果的に逆転させます。

各ポジションは固定の**Take Profit**と**Stop Loss**レベルで保護されています。
オプションの**Trailing Stop**は、価格が有利な方向に動いた後に価格を追跡し、利益を確保します。

戦略は完成したローソク足のみを処理し、インジケーターとローソク足サブスクリプションに高レベルAPIバインディングを使用します。

## パラメーター
- ローソク足タイプ
- ファストEMAの長さ
- スローEMAの長さ
- Take profit
- Stop loss
- Trailing stop
- Reverse
