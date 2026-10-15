# Altarius RSI Stochastic戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Altarius RSI Stochastic戦略は、MetaTrader 5のエキスパートアドバイザー「Altarius RSI Stohastic」をStockSharpの高レベルAPIに直接変換したものです。このシステムは2つのStochasticオシレーターを高速な3期間RSIと同期させ、モメンタムが圧縮された後に再び拡張するときに発生する短期間のリバーサルを捉えます。StockSharpの実装はオリジナルのエントリーとエグジットのロジックを維持しながら、戦略パラメーター、自動リスク管理、適応型ポジションサイジングなどの現代的な利便性を追加しています。

## 仕組み
- **プライマリStochastic (15/8/8):** トレンドフィルターとして機能します。ロングポジションには%Kラインが50以下で%Dラインを上抜けすることが必要で、中立からオーバーソールドゾーンでの上向きモメンタムを示します。ショートポジションには55以上での反対条件が必要です。
- **セカンダリStochastic (10/3/3):** %Kが%Dからどれだけ乖離しているかを測定します。ポジションに入る前にモメンタムを検証するために、最小5ポイントの絶対的な差が必要です。
- **RSI (期間3):** エグジットを制御します。RSIが60を超え、プライマリ%Dが70以上から下向きに転じたときにロングポジションをクローズします。RSIが40を下回り、プライマリ%Dが30以下から上向きに転じたときにショートポジションが終了します。
- **ドローダウンガード:** フローティングPnLが口座資産の設定可能なリスク倍数を下回ると、戦略はオープンポジションを即座に清算します。これはオリジナルコードの緊急ストップと同様です。
- **適応型サイジング:** 初期ボリュームはポートフォリオ資産に`MaximumRisk`係数を掛けて1000で割ることで算出され、MT5のアプローチと一致します。連続した負け取引は`DecreaseFactor`に従ってポジションサイズを縮小しますが、最小取引可能ボリュームを尊重します。

## パラメーター
| 名前 | 説明 | デフォルト値 |
| --- | --- | --- |
| `CandleType` | ローソク足サブスクリプションに使用する時間軸。 | 5分時間軸 |
| `BaseVolume` | ポートフォリオ情報が利用できない場合に使用するフォールバックボリューム。 | 0.1 |
| `MinimumVolume` | すべての計算後に許可される最小ボリューム。 | 0.1 |
| `MaximumRisk` | サイジングとドローダウンエグジットのためにポートフォリオ値に適用されるリスク乗数。 | 0.1 |
| `DecreaseFactor` | 連続した負け取引の後にボリュームを削減する除数。 | 3 |
| `PrimaryStochasticLength` | プライマリStochastic %Kラインのルックバック期間。 | 15 |
| `PrimaryStochasticKPeriod` | プライマリ%Kラインのスムージング。 | 8 |
| `PrimaryStochasticDPeriod` | プライマリ%Dシグナルラインの期間。 | 8 |
| `SecondaryStochasticLength` | 確認用Stochasticのルックバック期間。 | 10 |
| `SecondaryStochasticKPeriod` | セカンダリ%Kラインのスムージング。 | 3 |
| `SecondaryStochasticDPeriod` | セカンダリ%Dラインの期間。 | 3 |
| `DifferenceThreshold` | エントリーを許可するためのセカンダリ%Kと%Dの最小差。 | 5 |
| `PrimaryBuyLimit` | ロングを開く前に許可されるプライマリ%Kの最大値。 | 50 |
| `PrimarySellLimit` | ショートを開く前に許可されるプライマリ%Kの最小値。 | 55 |
| `PrimaryExitUpper` | ロングをクローズする前に超える必要があるプライマリ%Dしきい値。 | 70 |
| `PrimaryExitLower` | ショートをクローズする前に下回る必要があるプライマリ%Dしきい値。 | 30 |
| `RsiPeriod` | RSIルックバック長。 | 3 |
| `LongExitRsi` | ロングエグジットを確認するRSIレベル。 | 60 |
| `ShortExitRsi` | ショートエグジットを確認するRSIレベル。 | 40 |

## トレーディングルール
1. **エントリー条件**
   - **ロング:** プライマリ%K > プライマリ%D、プライマリ%K < `PrimaryBuyLimit`、|セカンダリ%K − セカンダリ%D| > `DifferenceThreshold`で戦略がフラット状態の場合。
   - **ショート:** プライマリ%K < プライマリ%D、プライマリ%K > `PrimarySellLimit`、|セカンダリ%K − セカンダリ%D| > `DifferenceThreshold`で戦略がフラット状態の場合。
2. **エグジット条件**
   - **ロングエグジット:** RSI > `LongExitRsi`、プライマリ%D > `PrimaryExitUpper`、現在の%D値が前のローソク足の値より低い。
   - **ショートエグジット:** RSI < `ShortExitRsi`、プライマリ%D < `PrimaryExitLower`、現在の%D値が前のローソク足の値より高い。
   - **リスクエグジット:** フローティング損失が`MaximumRisk × Portfolio.CurrentValue`を超えた場合。

## リスク管理
- 戦略はStockSharpの組み込みポジション保護サービスを有効にするために自動的に`StartProtection()`を呼び出します。
- `_lossStreak`が1回を超える連続した負け取引を超えると、MT5の`DecreaseFactor`ロジックを模倣してポジションサイズが縮小されます。
- `MinimumVolume`はポジションサイズが取引所のティックサイズ要件を下回らないようにします。

## 注意事項
- 戦略はオリジナルのEAと同様に、ヘッジング対応ポートフォリオを前提としています。
- MetaTraderで使用していた時間軸（M1、M5など）に合わせて`CandleType`パラメーターをカスタマイズしてください。
- このモジュールをStockSharp Designerまたはこのリポジトリのバックテスタープロジェクトと組み合わせて、独自のデータでパフォーマンスを検証してください。
