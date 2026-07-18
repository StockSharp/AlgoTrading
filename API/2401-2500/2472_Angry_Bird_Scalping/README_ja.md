# Angry Bird スキャルピング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はStockSharpの高レベルAPIを使用してMetaTraderのエキスパートアドバイザー「Angry Bird (Scalping)」を再現します。

## ロジック
- 15分足ローソク足を観察し、最後の`Depth`バーにわたる最高値と最安値を計算して動的グリッドステップを導出します。
- ポジションが開いておらず、前のローソク足が現在のものより高く終値した場合、時間足のRSIがエントリーを引き起こします: `RsiMin`より高い値はショートポジションを開き、`RsiMax`より低い値はロングポジションを開きます。
- ポジションが存在し、価格が少なくともグリッドステップ分逆行した場合、`MaxTrades`に達するまで同じ方向に`LotExponent`を掛けたボリュームで新しいポジションが開かれます。
- ショートに対して`CciDrop`を超える、またはロングに対して`-CciDrop`を下回る強いCCI値はすべてのポジションを強制決済します。
- 平均エントリー価格に対して利益が`TakeProfit`または損失が`StopLoss`に達した時もポジションを決済します。

## パラメーター
- `StopLoss` – ポイント単位のストップロス。
- `TakeProfit` – ポイント単位のテイクプロフィット。
- `DefaultPips` – グリッド注文間の最小距離（pips）。
- `Depth` – 高値/安値計算に使用するローソク足の数。
- `LotExponent` – 後続注文ボリュームの乗数。
- `MaxTrades` – 平均化ポジションの最大数。
- `RsiMin` / `RsiMax` – エントリーのRSIしきい値。
- `CciDrop` – ポジション決済を強制するCCIの絶対値。
- `Volume` – 初期注文ボリューム。
- `CandleType` – 作業ローソク足の時間軸（デフォルト15分）。

## 使用方法
戦略を銘柄に接続して開始します。戦略は成行注文を使用し、価格が逆行した時に平均化しながら単一のネットポジションを管理します。
