# 等距離チャネル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**等距離チャネル戦略**は、元の MQL4 エキスパートアドバイザー "Equidistant Channel" を StockSharp の高レベル API に移植したものです。この戦略は MACD ラインのクロスを分析し、Bollinger Bands への接触、breakeven ロジック、金額ベースの trailing 目標を通じて既存ポジションを管理します。

MACD ラインがシグナルを上抜けると、戦略はロングポジションを開き、シグナルを下抜けるとショートポジションを開きます。取引が有効な間、戦略は価格が Bollinger Bands に到達したとき、含み益が設定可能な金額または割合の目標に達したとき、または trailing drawdown のしきい値に違反したときのエグジットを監視します。breakeven モードは、利益が設定可能な価格ステップ数を超えると保護ストップを動かすことで、MetaTrader 実装を再現します。

## インジケーター
- **MACD (12, 26, 9)** - MACD ラインとそのシグナルラインのクロスでエントリーシグナルを生成します。
- **Bollinger Bands (20, 2)** - ローソク足の終値が上側または下側バンドに到達したときにエグジット水準を提供します。

## ポジション管理
- `StartProtection` を通じて価格ポイント単位で表される、任意の stop loss、take profit、trailing stop 距離。
- 商品の価格/ステップサイズのメタデータを使って含み益を追跡する、金額ベースの take profit と trailing ロジック。
- ポートフォリオ開始値から計算される割合ベースの take profit。
- 利益が定義済みトリガーに達したとき、ストップをエントリーにオフセットを加えた位置へ押し上げる breakeven モード。

## パラメーター
| グループ | 名前 | デフォルト | 説明 |
| --- | --- | --- | --- |
| 取引 | 数量 | 1 | 新規エントリーの注文数量。 |
| 一般 | ローソク足タイプ | 5 分 | 計算に使用するローソク足系列。 |
| インジケーター | MACD高速 | 12 | MACD の高速 EMA 長。 |
| インジケーター | MACD低速 | 26 | MACD の低速 EMA 長。 |
| インジケーター | MACDシグナル | 9 | MACD のシグナルライン長。 |
| インジケーター | BB期間 | 20 | Bollinger Bands のルックバック期間。 |
| インジケーター | BB偏差 | 2 | 標準偏差で表した Bollinger Bands の幅。 |
| リスク | Stop Loss | 20 | 価格ポイント単位の stop loss 距離。 |
| リスク | Take Profit | 50 | 価格ポイント単位の take profit 距離。 |
| リスク | Trailing Stop | 40 | 価格ポイント単位の trailing stop 距離。 |
| リスク | TP使用 (金額) | false | 含み益が絶対金額目標に達したときに決済します。 |
| リスク | TP金額 | 10 | 口座通貨での絶対 take profit 値。 |
| リスク | TP使用 (%) | false | 含み益が初期資本の割合に達したときに決済します。 |
| リスク | TP割合 | 10 | 割合ベース take profit に使う初期資本の割合。 |
| リスク | Trailing有効化 | true | 含み益に対する trailing ロジックを有効にします。 |
| リスク | Trailing起動 | 40 | trailing ロジックを準備する利益水準 (通貨)。 |
| リスク | Trailingステップ | 10 | 利益ピークから許容される最大 drawdown (通貨)。 |
| リスク | BB Stop使用 | true | 価格が Bollinger Bands に触れたときのエグジットを有効にします。 |
| リスク | Breakeven使用 | true | breakeven 動作を有効にします。 |
| リスク | Breakevenトリガー | 10 | breakeven stop を準備するために必要な利益 (価格ステップ)。 |
| リスク | Breakevenオフセット | 5 | breakeven 水準に適用されるオフセット (価格ステップ)。 |

## 注意事項
- 正確な金額計算のため、戦略は有効な `PriceStep` と `StepPrice` メタデータを提供する単一の商品で動作します。
- 利益 trailing モジュールは MetaTrader の動作に従います。含み益が起動しきい値を超えると、戦略は進行中の最大値を記録し、drawdown が設定された trailing ステップを超えると取引を決済します。
- breakeven ロジックは、価格ステップベースのトリガーとオフセットを使って元の EA を再現します。
- 戦略コード内のすべてのコメントは、プロジェクトのガイドラインに従って英語で書かれています。
