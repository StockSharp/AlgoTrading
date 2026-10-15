# Expert AutoLot 20/200戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、ユーザーが定義した時間に1日最大1ポジションをオープンします。過去の2本のバー（T1とT2）の始値を比較します。前のバーが後のバーよりDeltaShort pips高い場合にショートポジションをオープンします。後のバーがDeltaLong pips高い場合にロングポジションをオープンします。

ポジションのボリュームは固定または口座残高から自動計算することができます。前の取引と比較して残高が減少すると、ロットはBigLotSizeで乗算されます。

各取引には個別のテイクプロフィットとストップロス（pips単位）があります。また、最大保有時間（MaxOpenTime）により、指定した時間数が経過すると取引がクローズされます。

## パラメーター

- `CandleType` – 処理する足の時間軸（デフォルト：1時間）。
- `TradeHour` – エントリー条件を確認する時刻（時）。
- `T1`, `T2` – 始値比較のためのバーシフト。
- `DeltaLong`, `DeltaShort` – 始値の最小差（pips単位）。
- `TakeProfitLong`, `StopLossLong` – ロング取引の保護（pips単位）。
- `TakeProfitShort`, `StopLossShort` – ショート取引の保護（pips単位）。
- `Lot` – 基本取引ボリューム。
- `AutoLot` – 自動ロット計算を有効にする。
- `BigLotSize` – 損失後に適用される乗数。
- `MaxOpenTime` – ポジションを保有する最大時間（時間単位）。
