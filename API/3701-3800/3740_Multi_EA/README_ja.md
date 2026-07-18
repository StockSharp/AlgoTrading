# MultiStrategyEA v1.2 (StockSharp ポート)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、MetaTrader エキスパート アドバイザー **MultiStrategyEA v1.2** の上位レベルの StockSharp 移植です。オリジナルの EA は 7 つのオシレーターを集約し、複数の注文グリッドを管理します。 StockSharp バージョンはシグナル生成の側面に焦点を当てており、インジケーター モジュール間のコンセンサスによって駆動される単一のネット ポジションを取引します。 MT5 コードの注文管理、資金管理プロファイル、グリッド、リカバリ機能は、実装を StockSharp の高レベルの API と整合させ、明確さを維持するために意図的に省略されています。

## モジュール
この戦略は、選択した時間枠で次のインジケーター モジュールを評価します。

1. **加速/減速オシレーター (AC)** – Awesome Oscillator とその 5 周期 SMA の差を使用します。現在の値が `AcLevel` のしきい値を超え、前の読み取り値と比較して上昇 (または下降) する必要があります。
2. **平均方向指数 (ADX)** – ADX の強さが `AdxTrendLevel` を超え、支配的な方向の動きも `AdxDirectionalLevel` を超える場合の傾向を確認します。
3. **オーサム オシレーター (AO)** – オシレーターが `AoLevel` を超えて同じ方向に進み続けるときの運動量のバーストを検出します。
4. **DeMarker** – オシレーターが売られすぎ (`100 - DeMarkerThreshold`) または買われすぎ (`DeMarkerThreshold`) の領域を離れるときに反転の可能性をフラグします。
5. **フォース インデックス + Bollinger バンド** – フォース インデックス (MT5 スクリプトと同じようにポート内で正確にスケーリング) が `ForceConfirmationLevel` を超える勢いを確認している間に、価格が Bollinger バンドに達する必要があります。オプションの `BandDistanceFilter` は、ピップ単位で測定される帯域幅が狭すぎるか広すぎる場合に信号を拒否します。
6. **マネー フロー インデックス (MFI)** – DeMarker に似ています。 `MfiThreshold` によって決定される買われすぎゾーンと売られすぎゾーンに反応します。
7. **MACD + Stochastic** – MACD (`MacdLevel`) と Stochastic (`StochasticLevel`) の両方が同じ方向の偏りを確認することを要求します。 MACD は、そのレベルの上/下、および信号線の上/下にある必要があります。 Stochastic はしきい値を上回るか下回るか、信号線を上回るか下回る必要があります。

各モジュールは、最後に完成したローソク足に基づいて、**買い**、**売り**、または**中立**の投票に貢献します。

## コンセンサスロジック
- `TradeAllStrategies` が **true** (デフォルト) の場合、戦略はロングに入る前に、少なくとも `RequiredConfirmations` 件の強気票と **0** の弱気票が表示されるまで待機します。同じロジックがショートにも反映されます。
- `TradeAllStrategies` が **false** の場合、取引には 1 つの強気または弱気の投票で十分です。
- `CloseInReverse` が有効な場合、ストラテジーは新しいポジションをオープンする前に、反対のポジションを直ちにクローズします。

この実装は 1 つの集約ポジションのみを操作し、元の EA のモジュールごとの注文簿記を再作成しようとはしません。

## リスク管理
- `StopLossPips` と `TakeProfitPips` は、商品の `PriceStep` を使用して価格オフセットに変換されます。 10 進数が 3 桁または 5 桁のシンボルの場合、pip サイズは自動的に 10 倍になり、FX の pip 動作を模倣します。
- ストップとターゲットは、ローソクの高値/安値を使用して、完成したローソクごとにチェックされます。いずれかのしきい値に達すると、ポジション全体がクローズされます。

## MT5エキスパートアドバイザーとの違い
- グリッド、マーチンゲール、回復機能はありません。ポジションのサイジングは、`Volume` パラメータによって固定されます。
- クローズシグナルバリアント (MT5 の `CloseOrdersType` オプション) は実装されていません。エグジットは、グローバルなストップロス/テイクプロフィット、またはオプションの逆シグナル動作に依存します。
- StockSharp のインジケーター構成は各モジュールの主なアイデアを反映していますが、元のスクリプトにある多くのモード列挙ではなく、最も一般的な解釈のみをサポートしています。
- 資金管理ブロック (自動ロット、アカウント保護、シンボル固有の PIP 評価) は、この高レベル ポートの範囲外です。

## パラメーター
| パラメータ | 説明 |
|-----------|-------------|
| `CandleType` | すべてのインジケーター モジュールで使用されるデータ シリーズ。 |
| `Volume` | コンセンサスシグナルが現れたときの純取引高。 |
| `TradeAllStrategies` | コンセンサス投票を有効にします。それ以外の場合は、任意の 1 票が取引のトリガーとなります。 |
| `RequiredConfirmations` | コンセンサスが有効な場合に必要な、一致する強気投票または弱気投票の数。 |
| `CloseInReverse` | 反対側をオープンする前に、既存のポジションをクローズします。 |
| `StopLossPips` / `TakeProfitPips` | ピップ単位で測定されるプロテクティブストップと利益目標。 |
| `UseAcModule`, `AcLevel` | Accelerator Oscillator モジュールのトグルとしきい値。 |
| `UseAdxModule`, `AdxPeriod`, `AdxTrendLevel`, `AdxDirectionalLevel` | ADX 構成。 |
| `UseAoModule`, `AoLevel` | 素晴らしいオシレーター構成。 |
| `UseDeMarkerModule`, `DeMarkerPeriod`, `DeMarkerThreshold` | DeMarker オシレーターの設定。 |
| `UseForceBollingerModule`, `BollingerPeriod`, `BollingerDeviation`, `ForceConfirmationLevel`, `BandDistanceFilter` | インデックス + Bollinger バンド フィルター設定を強制します。 |
| `UseMfiModule`, `MfiPeriod`, `MfiThreshold` | マネーフローインデックスの設定。 |
| `UseMacdStochasticModule`, `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod`, `MacdLevel`, `StochasticPeriod`, `StochasticSignalPeriod`, `StochasticSlowing`, `StochasticLevel` | MACD と Stochastic のコンボ構成。 |

## 使用上の注意
1. すべての指標を形成するのに十分な履歴データを備えた商品に戦略を添付します。
2. 希望する市場状況に合わせてタイムフレームとモジュールのしきい値を設定します。デフォルトは、MT5 EA 入力で使用される値を複製します。
3. コンセンサス ロジックは、アクティブなモジュールの数に影響されます。モジュールを無効にする場合は、それに応じて `RequiredConfirmations` を下げることを検討してください。
4. この戦略は単一のネット ポジションを取引するため、追加のポートフォリオ ルーティングなしで、デザイナー、ランナー、またはその他の StockSharp の高レベル環境内での使用に適しています。

## 免責事項
この移植は、元の MetaTrader 専門家のリスクと資金管理スタック全体を再現するのではなく、信号のパリティに焦点を当てています。簡素化されたアーキテクチャにより、StockSharp ベースのソリューションへのテスト、拡張、統合が容易になりますが、複雑な機能 (グリッド、回収ロット、部分的なクローズ) が主なパフォーマンスの要因である場合、結果は MT5 バージョンとは異なります。
