# リペイントなし市場トレンドレベル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

オプションでRSIを使ってトレードをフィルタリングするEMAクロスオーバー戦略です。高速EMAが低速EMAを上抜けするとロングポジションが建ち、逆のクロスでショートトレードが発動します。`ApplyExitFilters`が有効でRSIフィルターがアクティブな場合、RSIが許容ゾーンを外れるとポジションが決済されます。

## 詳細

- **エントリー条件**:
  - **ロング**: `Fast EMA`が`Slow EMA`を上抜けし、有効時に`RSI > RsiLongThreshold`
  - **ショート**: `Fast EMA`が`Slow EMA`を下抜けし、有効時に`RSI < RsiShortThreshold`
- **エグジット条件**: 逆クロスオーバー、または`ApplyExitFilters`が真のときにRSIフィルターが失敗
- **タイプ**: トレンドフォロー
- **インジケーター**: EMA, RSI
- **時間軸**: 5分（デフォルト）
