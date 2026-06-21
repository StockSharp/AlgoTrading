# Exp Leading戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、John F. Ehlersが*Cybernetics Analysis for Stock and Futures*で説明しているカスタム**Leading**インジケーターに基づくクロスオーバーシステムを実装しています。インジケーターは2本のラインを計算します：

1. **NetLead** – `Alpha1`と`Alpha2`係数で制御される平滑化リーディングフィルター。
2. **EMA** – 定数係数0.5を持つシンプルな指数移動平均。

戦略は選択された時間軸の確定済みローソク足で動作します。NetLadラインがEMAラインを**下方に**クロスすると、上昇反転が予測されてロングポジションが開かれます。逆に、NetLeadがEMAラインを**上方に**クロスすると、ショートポジションが開かれます。既存のポジションがある場合、反対注文が送信されると暗黙的に決済されます。

## パラメーター

- `Alpha1` – 中間リーディング計算の係数。デフォルト: `0.25`。
- `Alpha2` – リーディング結果に適用される平滑化係数。デフォルト: `0.33`。
- `CandleType` – 計算に使用するローソク足データタイプ。デフォルト: 4時間の時間軸。
- `StopLoss` – 絶対価格単位のストップロス。デフォルト: `1000`。
- `TakeProfit` – 絶対価格単位のテイクプロフィット。デフォルト: `2000`。

## トレードロジック

1. 各確定ローソク足でNetLeadとEMAの値が更新されます。
2. 前のバーがEMAより上のNetLeadを示し、最新のバーがEMAより下のNetLeadを示す場合、**買い**成行注文が送信されます。
3. 前のバーがEMAより下のNetLeadを示し、最新のバーがEMAより上のNetLeadを示す場合、**売り**成行注文が送信されます。
4. `StartProtection`はストップロスとテイクプロフィットのルールを自動的に適用するために使用されます。

この例は教育目的であり、MetaTrader戦略をStockSharpの高レベルAPIに移植する方法を示しています。
