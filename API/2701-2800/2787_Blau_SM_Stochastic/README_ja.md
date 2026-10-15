# Blau SM Stochastic 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、Blau SM Stochastic オシレーターを中心に構築された元の MetaTrader 5 エキスパート `Exp_BlauSMStochastic` の C# 変換です。インジケーターは価格と最近の取引レンジの距離を測定し、複数のスムージングステージを適用し、その結果をスムージングされた参照ラインと比較します。戦略は完成したローソク足（デフォルトは 4 時間時間軸）で動作し、両方向の取引を許可します。

## インジケーターロジック
1. `LookbackLength` バーにわたる最高値と最低値を計算します。
2. トレンド除去価格シリーズを構築します：`sm = price - (HH + LL) / 2`（`price` は適用された価格タイプ）。
3. 選択された `SmoothMethod`（SMA、EMA、SMMA、LWMA）を使用して、`FirstSmoothingLength`、`SecondSmoothingLength`、`ThirdSmoothingLength` の長さで 3 つの移動平均によりトレンド除去シリーズを順次スムージングします。
4. ボラティリティを正規化するために同じ三重シーケンスでハーフレンジ `(HH - LL) / 2` をスムージングします。
5. メインオシレーターラインを `100 * smoothed(sm) / smoothed(range)` として形成します。
6. シグナルラインを得るために `SignalLength` でメインラインをスムージングします。

`Phase` パラメーターは MQL バージョンとの互換性のために保持されますが、簡略化されたスムージングエンジンでは使用されません。

## 取引モード
- **Breakdown**：メインラインのゼロクロッシングを監視します。正から非正へのクロッシングはロングを開き、ショートをクローズします。負から非負へのクロッシングはショートを開き、ロングをクローズします。
- **Twist**：モメンタムのねじれを追跡します。メインラインが局所的なトラフを形成する場合（下落後に値が上昇）、ロングエントリーがトリガーされ、局所的なピーク（上昇後に値が下落）はショートをトリガーします。逆方向のポジションはそれに応じてクローズされます。
- **CloudTwist**：メインラインとシグナルラインの間のクロッシングを観察します。メインラインがシグナルラインを下向きにクロスするとロングを開いてショートをクローズし、上向きのクロスはショートを開いてロングをクローズします。

エントリーと決済スイッチ（`EnableLongEntry`、`EnableShortEntry`、`EnableLongExit`、`EnableShortExit`）は、インジケーターの計算を維持したまま特定の操作を無効にすることができます。

## リスク管理
`TakeProfitPoints` と `StopLossPoints` は銘柄の価格ステップを使用して絶対価格距離に変換され、`StartProtection` を通じて組み込みの保護ブロックに渡されます。対応するリミットを無効にするにはゼロに設定してください。

## パラメーター
- `CandleType` *(DataType、デフォルト：4 時間の時間軸)* – ローソク足サブスクリプションとインジケーター計算に使用する時間軸。
- `Mode` *(BlauSmStochasticModes、デフォルト：Twist)* – シグナル生成モードを選択（Breakdown、Twist、CloudTwist）。
- `SignalBar` *(int、デフォルト：1)* – シグナルを評価するときにインジケーター値をシフトするバー数、元の `SignalBar` ロジックを再現。
- `LookbackLength` *(int、デフォルト：5)* – 最高値と最低値を計算するために使用するバー。
- `FirstSmoothingLength` *(int、デフォルト：20)* – 最初のスムージングステージの長さ。
- `SecondSmoothingLength` *(int、デフォルト：5)* – 2 番目のスムージングステージの長さ。
- `ThirdSmoothingLength` *(int、デフォルト：3)* – 3 番目のスムージングステージの長さ。
- `SignalLength` *(int、デフォルト：3)* – シグナルラインのスムージング長。
- `SmoothMethod` *(BlauSmSmoothMethods、デフォルト：EMA)* – すべてのスムージングステージに適用される移動平均ファミリー（SMA、EMA、SMMA、LWMA）。
- `PriceType` *(BlauSmAppliedPrices、デフォルト：Close)* – オシレーターに使用する適用価格（終値、始値、高値、安値、中央値、典型的価格、加重価格、シンプル価格、四分値、トレンドフォロー変形、Demark）。
- `EnableLongEntry` *(bool、デフォルト：true)* – ロングポジションの開設を許可。
- `EnableShortEntry` *(bool、デフォルト：true)* – ショートポジションの開設を許可。
- `EnableLongExit` *(bool、デフォルト：true)* – ロングポジションのクローズを許可。
- `EnableShortExit` *(bool、デフォルト：true)* – ショートポジションのクローズを許可。
- `TakeProfitPoints` *(int、デフォルト：2000)* – 銘柄ポイントで表現された固定テイクプロフィット距離。
- `StopLossPoints` *(int、デフォルト：1000)* – 銘柄ポイントで表現された固定ストップロス距離。

## 注記
- スムージングエンジンは現在クラシックな移動平均（SMA、EMA、SMMA、LWMA）をサポートしています。MQL ライブラリのエキゾチックモード（JMA、JurX 等）は StockSharp では利用できないため含まれていません。
- Phase は完全性のためにパラメーターとして保持されています；ドキュメント目的のみで調整してください。
- StockSharp でサポートされているあらゆるシンボルで動作します。銘柄のボラティリティに合わせてローソク足タイプ、スムージング長、ストップを調整してください。
