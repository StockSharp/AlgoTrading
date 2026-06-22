# セッション注文センチメント戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、注文板で観察される買い注文と売り注文の不均衡に基づいて取引します。板の両側の注文数と合計出来高の比率を測定し、一方の側の優勢が設定可能な閾値を超えたときにポジションを開きます。取引は指定された時間帯のみ許可されます。

ポジションを開いた後、反対側を監視するために閾値が下げられます。反対側がこれらの下げられた閾値を超えて成長した場合、ポジションが閉じられます。絶対価格ポイントを使ったストップロスとテイクプロフィットも適用されます。

## 取引ルール
- **ロングエントリー**: 以下の場合に買い
  - `BUY volume / SELL volume >= DiffVolumesEx` かつ `BUY orders / SELL orders >= DiffTradersEx`
  - どちらかの側が `MinTraders` と `MinVolume` を満たす
  - 現在の時刻が `CheckTradingTime` を通過している
- **ショートエントリー**: 上記ロジックが売り側に対してミラーリングされる場合に売り。
- **エグジット**:
  - `SELL volume / BUY volume > 1 / DiffVolumes` または `SELL orders / BUY orders > 1 / DiffTraders` の場合にロングを閉じる
  - `SELL volume / BUY volume < DiffVolumes` または `SELL orders / BUY orders < DiffTraders` の場合にショートを閉じる
  - 取引時間外にすべてのポジションを閉じる
- **ストップ**: 価格ポイントで測定した `Stop Loss` と `Take Profit` を使用。

## パラメーター
- `MinVolume` – 板のいずれかの側に必要な最小合計出来高（デフォルト：20000）
- `MinTraders` – いずれかの側の最小注文数（デフォルト：1000）
- `DiffVolumesEx` – エントリーに必要な出来高比率（デフォルト：2.0）
- `DiffTradersEx` – エントリーに必要な注文数比率（デフォルト：1.5）
- `MinDiffVolumesEx` – ポジション開設後に使用する出来高比率（デフォルト：1.5）
- `MinDiffTradersEx` – ポジション開設後に使用する注文数比率（デフォルト：1.3）
- `SleepMinutes` – 注文板チェックの間隔（分）（デフォルト：5）
- `TpPips` – 価格ポイントでのテイクプロフィット（デフォルト：500）
- `SlPips` – 価格ポイントでのストップロス（デフォルト：500）

## 備考
この戦略にはPythonバージョンは含まれません。
