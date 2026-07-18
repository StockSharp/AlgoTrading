# Color HMA リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Hull Moving Averageの傾きの変化に基づく戦略。HMAが反転したとき、新しい方向に逆らったポジションを決済し、トレンドに沿ったポジションを開きます。

## パラメーター
- `HmaPeriod` — Hull Moving Averageの期間。
- `CandleType` — 使用するローソク足の種類。
- `BuyOpen`, `SellOpen` — ロング/ショートポジションの開設を許可。
- `BuyClose`, `SellClose` — ロング/ショートポジションの決済を許可。

## シグナル
- **上方リバーサル**: 前のHMAが下落していて現在の値が上昇 → ショートを決済してロングを開設。
- **下方リバーサル**: 前のHMAが上昇していて現在の値が下落 → ロングを決済してショートを開設。

戦略はマーケットオーダーを使用し、`Strategy.Volume`で指定されたボリュームで取引します。
