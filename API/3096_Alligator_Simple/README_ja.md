# シンプルAlligator戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
シンプルAlligator戦略は、StockSharpの高レベルAPIを使用してMetaTraderの「Alligator Simple v1.0」エキスパートアドバイザーを再現します。完了ローソク足でBill WilliamsのAlligatorインジケーターを読み取り、前の完了バーでLips、Teeth、Jawラインが同じ方向に拡張したときにポジションを開きます。各トレードには、元のMQL実装を反映したpipベースのストップロス、テイクプロフィット、トレーリングストップ管理をオプションで含めることができます。

## インジケーターとデータ
- **Alligatorライン**: Jaw、Teeth、Lipsの設定可能な長さと前方シフトを持つローソク足の中央価格`(high + low) / 2`で計算された3つのSmoothed Moving Average（SMMA）。
- **ローソク足**: 戦略は1つの設定可能な`CandleType`（デフォルトで1時間ローソク足）を購読し、先読みバイアスを避けるために完了したローソク足のみを処理します。

## トレードロジック
1. **シグナル評価**
   - 前の完了ローソク足のシフトされたAlligator値を取得します。
   - ロングシグナル：`Lips[t-1] > Teeth[t-1] > Jaw[t-1]`。
   - ショートシグナル：`Lips[t-1] < Teeth[t-1] < Jaw[t-1]`。
2. **実行**
   - ポジションが開いていない場合、`OrderVolume`で市場に入ります。
   - 一度に1つのポジションのみを保持します；現在のポジションが決済されるまで反対のシグナルは無視されます。

## エグジットとリスク管理
- **初期ストップロス**: `StopLossPips > 0`の場合、戦略は銘柄の価格ステップで変換したpip距離分（MetaTraderシンボルが使用する3/5桁pip乗数を含む）だけ執行価格をオフセットします。
- **テイクプロフィット**: `TakeProfitPips > 0`の場合、利益目標がエントリー価格の周囲に対称的に配置されます。ゼロ値は目標を無効にします。
- **トレーリングストップ**: `TrailingStopPips`と`TrailingStepPips`の両方が正の場合、価格がトレードに有利な方向に少なくとも`TrailingStop + TrailingStep`動くと、ストップが`close − TrailingStop`（ロング）または`close + TrailingStop`（ショート）に進みます。トレーリング更新はバー内タッチをシミュレートするためにローソク足の高値/安値に依存します。
- **エグジット処理**: ストップロス、テイクプロフィット、トレーリングの条件はポジションをフラットにする成行注文を発し、完了したすべてのローソク足で評価されます。

## パラメーター
- `OrderVolume`（デフォルト**1**）：ロットまたはコントラクトのトレードサイズ。
- `StopLossPips`（デフォルト**100**）：pipsでの初期ストップロス距離。無効にするにはゼロに設定します。
- `TakeProfitPips`（デフォルト**100**）：pipsでのテイクプロフィット距離。無効にするにはゼロに設定します。
- `TrailingStopPips`（デフォルト**5**）：pipsでのトレーリングストップ距離。ゼロはトレーリングを無効にします。
- `TrailingStepPips`（デフォルト**5**）：トレーリングストップが進む前に価格が移動する必要がある追加pip距離。トレーリングが有効な場合は正である必要があります。
- `JawPeriod`、`TeethPeriod`、`LipsPeriod`：jaw、teeth、lipsのSMMA長さ（デフォルト13/8/5）。
- `JawShift`、`TeethShift`、`LipsShift`：Alligator値を読み取る際に適用される前方シフト（デフォルト8/5/3）。
- `CandleType`：計算のためのローソク足データタイプ/時間軸（デフォルト1時間ローソク足）。

## 実装上の注意
- pip距離は証券のティックサイズに自動的に適応します。3桁または5桁の小数を持つ銘柄は、MetaTraderのpip定義を複製するために価格ステップを10倍します。
- インジケーター履歴バッファは設定された前方シフトを尊重するのに十分な値を保存し、手動配列操作を排除します。
- 戦略は注文を送信するために`BuyMarket`と`SellMarket`ヘルパーを使用し、コードをシグナル生成とリスク処理に集中させます。
