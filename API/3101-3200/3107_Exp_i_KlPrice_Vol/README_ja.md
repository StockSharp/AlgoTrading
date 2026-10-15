# Exp i-KlPrice Vol 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、MetaTraderエキスパート**Exp_i-KlPrice_Vol.mq5**のC#変換です。価格とボラティリティバンドの間の距離を測定する
KlPriceオシレーターを再構築し、オシレーターを足のボリュームで乗算し、適応閾値によって生成されるカラー遷移を追跡します。
元のエキスパートアドバイザーのデュアルマジック動作を反映して、各方向に2つの独立したポジションスロットがエミュレートされます。

## インジケーターロジック
- 価格は選択された`AppliedPrice`モード（close、open、median、Demakなど）を使用して変換されます。
- 変換された価格は`PriceMaMethod`と`PriceMaLength`で定義された移動平均法で平滑化されます。
- 足の値幅（`High - Low`）は`RangeMaMethod`/`RangeMaLength`で平滑化されます。値幅は動的なバンド幅として機能します。
- ベースKlPriceオシレーターは`100 * (Price - (MA - RangeMA)) / (2 * RangeMA) - 50`として計算されます。
- オシレーターは選択されたボリュームソース（`AppliedVolume.Tick`または`AppliedVolume.Real`）で乗算されます。
- 長さ`SmoothingLength`のJurikスムーザーがオシレーターと生ボリュームの両方に適用され、2つの適応シリーズを作成します。
- 適応閾値は平滑化されたボリュームに`HighLevel2`、`HighLevel1`、`LowLevel1`、`LowLevel2`を掛けることで得られます。
- 現在のオシレーターカラーは、平滑化されたオシレーター値を適応閾値と比較して決定されます：
  - **4** – `HighLevel2 * volume`より上（極端な強気圧力）。
  - **3** – `HighLevel1 * volume`と極端なレベルの間。
  - **2** – 強気と弱気の閾値の間。
  - **1** – 下閾値と中立線の間。
  - **0** – `LowLevel2 * volume`より下（極端な弱気圧力）。

## 取引ルール
1. `SignalBar`（通常は前の完成した足）のカラーとその前のカラーを評価する。
2. ロングエントリー：
   - `AllowLongEntry`が`true`のとき、カラーが**4**から**4**未満の任意の値に変わるとスロット1が開く。
   - カラーが**3**から**3**未満に変わるとスロット2が開く。
3. ショートエントリー：
   - `AllowShortEntry`が`true`のとき、カラーが**0**から**0**超に上昇するとスロット1が開く。
   - カラーが**1**から**1**超に上昇するとスロット2が開く。
4. 以前のカラーが**0**または**1**で`AllowLongExit`が有効な場合、ロング決済が発生する。
5. 以前のカラーが**4**または**3**で`AllowShortExit`が有効な場合、ショート決済が発生する。
6. 各スロットは同じ足での重複注文を避けるために最後のシグナル時間を追跡する。保護ストップはオプションで、
   `StopLossPoints`または`TakeProfitPoints`がゼロより大きい場合に`StartProtection`を通じて処理される。

## パラメーター
| 名前 | 型 | デフォルト | 説明 |
|-----|-----|----------|------|
| `PrimaryVolume` | `decimal` | `0.1` | 最初のロング/ショートスロットで使用するボリューム。 |
| `SecondaryVolume` | `decimal` | `0.2` | 2番目のスロットで使用するボリューム。 |
| `StopLossPoints` | `int` | `1000` | 価格ステップ単位のオプション保護ストップ距離。 |
| `TakeProfitPoints` | `int` | `2000` | 価格ステップ単位のオプションテイクプロフィット距離。 |
| `AllowLongEntry` | `bool` | `true` | ロングポジションの開設を有効化。 |
| `AllowShortEntry` | `bool` | `true` | ショートポジションの開設を有効化。 |
| `AllowLongExit` | `bool` | `true` | 弱気カラーが現れたときにロングポジションを閉じる。 |
| `AllowShortExit` | `bool` | `true` | 強気カラーが現れたときにショートポジションを閉じる。 |
| `CandleType` | `DataType` | `H8` | 計算用の足のタイムフレーム。 |
| `PriceMaMethod` | `SmoothMethod` | `Sma` | 適用価格に使用する移動平均のタイプ。 |
| `PriceMaLength` | `int` | `100` | 価格スムーザーの長さ。 |
| `PriceMaPhase` | `int` | `15` | Jurikベースフィルターの位相パラメーター。 |
| `RangeMaMethod` | `SmoothMethod` | `Jjma` | 足値幅に使用する移動平均のタイプ。 |
| `RangeMaLength` | `int` | `20` | 値幅スムーザーの長さ。 |
| `RangeMaPhase` | `int` | `100` | 値幅スムーザーの位相パラメーター。 |
| `SmoothingLength` | `int` | `20` | オシレーターとボリュームに適用されるJurik平滑化長さ。 |
| `AppliedPrice` | `AppliedPrice` | `Close` | オシレーター計算に使用する価格ソース。 |
| `VolumeType` | `AppliedVolume` | `Tick` | オシレーターで乗算するボリュームソース。 |
| `HighLevel2` | `int` | `150` | 適応閾値の上部極端乗数。 |
| `HighLevel1` | `int` | `20` | 上部中程度乗数。 |
| `LowLevel1` | `int` | `-20` | 下部中程度乗数。 |
| `LowLevel2` | `int` | `-150` | 下部極端乗数。 |
| `SignalBar` | `int` | `1` | カラー遷移を読むために使用される履歴オフセット。 |

## 使用上の注意
- 価格とボリュームの両方の情報を提供する銘柄に戦略を接続してください；ティックボリュームのみが利用可能な場合、ティック
  カウンターがプロキシとして使用されます。
- 2つのスロットボリュームは、元のEAのデュアルマネー管理設定をエミュレートするために独立して調整できます。
- 部分的に形成された足を扱う場合や、履歴データを再同期する場合は`SignalBar`を調整してください。
- 平滑化メソッドはMQL`SmoothAlgorithms`ライブラリの動作を複製するためにリフレクションを通じてJurikフィルターをサポートします。
- `StartProtection`はストップまたはターゲット距離が正の場合にのみ呼び出されるため、保護注文を無効にするにはゼロのままにしてください。
