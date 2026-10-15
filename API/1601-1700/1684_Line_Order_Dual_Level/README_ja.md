# ダブルレベル注文戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderエキスパートアドバイザー「MyLineOrder」をStockSharp API向けに適応したものです。トレーダーは水平価格レベルを定義でき、価格がそれらに触れると自動的に成行注文が発動します。オプションのストップロス、テイクプロフィット、トレーリングストップの距離はpipsで表され、取引量は設定可能です。

市場価格が**BuyPrice**レベルに達すると、戦略はロングポジションに入ります。**SellPrice**レベルに触れるとショートポジションが開かれます。エントリー後、戦略はポジションを監視し、保護条件のいずれか（ストップロス、テイクプロフィット、またはトレーリングストップ）が満たされると決済します。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格が`BuyPrice`に触れるか超える。
  - **ショート**: 価格が`SellPrice`に触れるか下回る。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - ストップロス、テイクプロフィット、またはトレーリングストップ。
- **ストップ**:
  - `StopLossPips`、`TakeProfitPips`、`TrailingStopPips`。
- **フィルター**:
  - なし。
- **パラメーター**:
  - `BuyPrice` – ロングエントリーのレベル。
  - `SellPrice` – ショートエントリーのレベル。
  - `StopLossPips` – ストップロス距離（pips）。
  - `TakeProfitPips` – テイクプロフィット距離（pips）。
  - `TrailingStopPips` – トレーリングストップ距離（pips）。
  - `TradeVolume` – 注文数量。
  - `CandleType` – 価格監視に使用するローソク足の時間軸。
