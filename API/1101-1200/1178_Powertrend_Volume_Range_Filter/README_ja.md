# Powertrend - 出来高レンジフィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は出来高調整レンジフィルターを使用して動的な価格帯を定義します。アップトレンド中に価格が上限バンドを突破するとロングエントリーが発生し、価格が下限バンドを下回るとポジションが決済されます。オプションのフィルターにはADX、高値/安値によるトレンド確認、フィルター値のVWMAが含まれます。

## 計算と執行

C# と Python は次の明示的な因果定義を使います。確定足で `d = abs(Close - 前の Close)`、`V = TotalVolume` とします。`RangePeriod` 回の変化が揃った後、現在足を含む窓で `r = RangeMultiplier * sum(d * V) / sum(V)` を計算し、合計出来高は正とします。最初の形成済みフィルター `f` は現在の終値です。その後は前の `f` を `[Close - r, Close + r]` に制限します。現在の判定バンドは中心更新前の `前の f + r` と `前の f - r` です。これにより、フィルターを移動した後のバンドを突破価格そのものと定義する問題を避けます。

ポジションなしで `Close > 上限バンド` かつフィルター上昇時だけ `Volume` を買います。`Close < 下限バンド` ならエントリーフィルターに関係なく全ロングを成行決済します。同値はポジション維持、未完了注文中は追加注文を禁止します。ショート、EMA 交差、待機時間はありません。

任意の ADX は形成済みで `AdxThreshold` より厳密に高い必要があります。無効な ADX は登録せず、その準備完了を待ちません。高値・安値確認は `現在の High > 前の High` と `現在の Low > 前の Low` を同時に要求します。フィルター値の VWMA は現在値を含む直近 `VwmaPeriod` 個の形成済み値の `sum(f * V) / sum(V)` です。完全な窓、正の合計出来高、`Close > VWMA` が必要です。無効なフィルターは準備完了を要求しません。これは確定足での成行判断であり、取引所のストップ注文や足内の高値・安値での想定約定ではありません。

標準値：`RangePeriod=40`、`RangeMultiplier=1.5`、`AdxPeriod=14`、`AdxThreshold=20`、`VwmaPeriod=20`、`CandleType=TimeSpan.FromMinutes(5).TimeFrame()`。`IsAdxFilterEnabled`、`IsHighLowFilterEnabled`、`IsVwmaFilterEnabled` はすべて `false`。期間と倍率は正、ADX 閾値は `[0, 100]` 内にします。この式と値が本例を明確化します。元の短い説明には原指標の式がありませんでした。
