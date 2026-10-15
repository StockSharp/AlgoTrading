# Coensio Swing Trader V06戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はオリジナルのCoensio Swing Traderのブレイクアウトロジックを再現します。ドンチャンチャネルを使用して動的なサポートとレジスタンスを定義します。価格が設定可能なしきい値だけ上限バンドを上抜けるか、下限バンドを下抜けたときにトレードが開始されます。

## 詳細

- **エントリー**:
  - **ロング**: 終値がドンチャンチャネルの上限バンド + `Entry Threshold` pipsを上抜けた場合。
  - **ショート**: 終値がドンチャンチャネルの下限バンド - `Entry Threshold` pipsを下抜けた場合。
- **エグジット**:
  - エントリー価格から計測したpips単位の固定`Stop Loss`と`Take Profit`。
  - `Break Even` pipsの利益後にブレイクイーブンへの移動（オプション）。
  - ブレイクイーブン後に価格を`Trailing Step` pips追従するトレーリングストップ（オプション）。
- **ストップ**: ストップロス、テイクプロフィット、ブレイクイーブン、トレーリングストップ。
- **デフォルト値**:
  - `Channel Period` = 20
  - `Entry Threshold` = 15 pips
  - `Stop Loss` = 50 pips
  - `Take Profit` = 80 pips
  - `Break Even` = 25 pips
  - `Trailing Step` = 5 pips
  - `Enable Trailing` = false
  - `Candle Type` = 15分足ローソク足
