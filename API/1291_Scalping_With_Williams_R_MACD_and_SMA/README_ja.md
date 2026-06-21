# Williams %R、MACD、SMA を使ったスキャルピング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

1分足ローソク足で Williams %R、MACD ヒストグラム、単純移動平均を組み合わせたスキャルピング戦略です。

## 詳細

- **エントリー条件**: Williams %R が活性化レベルをクロスし、MACD ヒストグラムがトレンド方向に符号を変える。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ヒストグラムが方向を反転する。
- **ストップ**: なし。
- **デフォルト値**:
  - `WilliamsLength` = 140
  - `MacdFast` = 24
  - `MacdSlow` = 52
  - `MacdSignal` = 9
  - `SmaLength` = 7
