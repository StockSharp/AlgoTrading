# Awesome Oscillator Trader戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Awesome Oscillator Trader戦略は、MetaTraderのエキスパートアドバイザー「AwesomeOscTrader」を直接変換したものです。Bill WilliamsのAwesome Oscillatorに、ボリンジャーバンド幅とストキャスティクスのフィルターを組み合わせ、深いモメンタム収縮後のブレイクアウトを狙います。元の推奨を反映し、EURUSDなど流動性の高いFXペアの単一シンボル・1時間足取引用に設計されています。

戦略は、ボリンジャーバンドのスプレッドが設定可能な範囲に入るのを待ち、ボラティリティが消えずに収縮したことを示します。そのスクイーズ中、Awesome Oscillatorヒストグラムは特徴的な5本バー反転パターンを形成する必要があります。ゼロ未満に留まる4本連続の下降ヒストグラムバーに続き、新しいバーがまだ負のまま上昇色へ変わる構造です。この構造が形成され、ストキャスティクスが売られ過ぎ水準を上抜けると、スクイーズが上方向に解けることを期待してロングを開きます。逆に、ゼロを上回る4本の正の上昇ヒストグラムバーと、まだ正のまま下降色へ変わる新バーに、ストキャスティクスが上側しきい値を下回る動きが組み合わさると、ショートエントリーを発動します。

ポジションはATRベースのストップ距離で保護されます。各バーで3期間Average True Rangeを読み取り、設定可能な係数を掛け、銘柄のティックサイズに基づいてpipsへ変換します。その値が初期ストップロスとテイクプロフィット目標の両方を定義し、MetaTrader版の対称的な決済ロジックを再現します。任意のトレーリングストップは、価格が設定pipsだけ有利に動くと保護水準を引き締めます。`CloseOnReversal`は、反対のAwesome Oscillatorパターンまたは色変更が現れたときにポジションを閉じます。利益フィルターにより、反転シグナルで勝ちトレードのみ、負けトレードのみ、またはすべてを閉じることができ、EAの`ProfitTypeClTrd`動作を再現します。

## 取引ルール

- **時間軸:** 既定は1時間足（完全に設定可能）。
- **フィルター:**
  - ボリンジャーバンド幅は`BollingerSpreadLower`から`BollingerSpreadUpper` pipsの間である必要があります。
  - Stochastic %Kは、ロングでは`StochasticLowerLevel`、ショートでは`StochasticUpperLevel`と比較されます。
  - Awesome Oscillatorは、最新バーが色を変えつつゼロの反対側に残る5本バー反転構造を作る必要があり、正規化された大きさが`AoStrengthLimit`を超える必要があります。
- **エントリー:**
  - **ロング:** 上記条件に加え、現在バーが許可された取引時間窓内にある。
  - **ショート:** 反対条件。
- **決済:**
  - ATR由来のストップロスとテイクプロフィットをエントリー時に対称設定。
  - トレーリングストップ（`TrailingStopPips` &gt; 0の場合）は利益方向へ切り上げ/切り下げ。
  - `CloseOnReversal`と`ProfitFilter`に応じて、反対シグナルまたはオシレーター色変更で任意に決済。

## 主要パラメーター

| パラメーター | 既定値 | 説明 |
|-----------|---------|-------------|
| `CandleType` | 1時間 | すべての指標で使う時間軸。 |
| `BollingerPeriod` | 20 | ボリンジャーバンド・ボラティリティフィルターの期間。 |
| `BollingerSigma` | 2.0 | ボリンジャーバンドの標準偏差乗数。 |
| `BollingerSpreadLower` | 24 pips | 取引に必要な最小バンドスプレッド。 |
| `BollingerSpreadUpper` | 230 pips | 許容される最大バンドスプレッド。 |
| `AoFastPeriod` / `AoSlowPeriod` | 4 / 28 | Awesome Oscillatorの高速/低速期間。 |
| `AoStrengthLimit` | 0.0 | エントリー確認に必要な最小正規化AO大きさ。 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | 1 / 4 / 1 | MetaTrader既定値を再現するストキャスティクス長。 |
| `StochasticLowerLevel` / `StochasticUpperLevel` | 12 / 21 | シグナル確認用の売られ過ぎ/買われ過ぎしきい値。 |
| `EntryHour` / `OpenHours` | 16 / 13 | 取引時間窓の開始時刻と長さ。EA同様に深夜跨ぎを処理します。 |
| `RiskPercent` | 0.5% | 口座データが利用可能な場合のポジションサイズ計算用リスク率。 |
| `AtrMultiplier` | 4.5 | ストップ距離算出のため3期間ATRに適用する乗数。 |
| `TrailingStopPips` | 40 pips | 任意トレーリングストップ距離（0で無効）。 |
| `ProfitFilter` | OnlyProfitable | 反転決済で、任意/利益のみ/損失のみのどれを閉じるかを選択。 |
| `MaxOpenOrders` | 1 | 同時ポジション最大数（EAに合わせて1）。 |

## 実装メモ

- StockSharpの`BollingerBands`、`StochasticOscillator`、`AwesomeOscillator`、`AverageTrueRange`、`Highest`指標を使い、手動指標計算はありません。
- AO値は直近100バーで正規化され、MetaTrader指標バッファーを模倣し、カスタムコードなしで色ロジックを再現します。
- ポジションサイズは、利用可能な場合`Security.StepVolume`、`Security.MinVolume`、`Security.MaxVolume`、`Security.StepPrice`を尊重し、そうでなければ戦略の既定数量へフォールバックします。
- 保護水準は完全に戦略内で管理されます。ストップとテイクプロフィットのチェックは各確定ローソク足で実行され、ブローカー側注文を必要とせずEAのティックレベル管理に合わせます。
- コード内のコメントはすべて英語で、インデントはプロジェクトガイドラインに従いタブを使用します。
