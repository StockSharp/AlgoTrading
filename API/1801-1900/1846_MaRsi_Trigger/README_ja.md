# MA RSIトリガー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は高速・低速の指数移動平均（EMA）とRSIを組み合わせてトレンド転換を検出します。
高速EMAと高速RSIが共にその低速版より上にある場合、市場を強気として扱いロングポジションをオープンします。
両方が下にある場合、ショートポジションをオープンします。パラメーターでロング・ショートのエントリーやエグジットを有効・無効にできます。

## 詳細

- **エントリー条件**:
  - **ロング**: 高速EMA > 低速EMA かつ 高速RSI > 低速RSI（前回のトレンドが弱気）。
  - **ショート**: 高速EMA < 低速EMA かつ 高速RSI < 低速RSI（前回のトレンドが強気）。
- **エグジット条件**:
  - **ロング**: トレンドが弱気になりロングエグジットが許可されている場合。
  - **ショート**: トレンドが強気になりショートエグジットが許可されている場合。
- **インジケーター**: EMA, RSI。
- **ストップ**: 含まれていません。
- **時間軸**: デフォルトで4時間足。
- **パラメーター**:
  - `FastRsiPeriod` = 3
  - `SlowRsiPeriod` = 13
  - `FastMaPeriod` = 5
  - `SlowMaPeriod` = 10
  - `AllowBuyEntry` = true
  - `AllowSellEntry` = true
  - `AllowLongExit` = true
  - `AllowShortExit` = true
