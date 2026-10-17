# 季節性調整モメンタム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

モメンタムと過去の月次リターンを組み合わせます。確定5分足で `M = Momentum(14)` と M の `A = SMA(14)` を計算します。建玉なしで `S >= SeasonalityThreshold` かつ `M > A` なら買い、`S <= -SeasonalityThreshold` かつ `M < A` なら売りです。既定の `SeasonalityThreshold = 0.5` の単位はパーセントポイントです。

ロングは季節条件を満たさなくなるか `M < A`、ショートは条件不成立か `M > A` で決済します。各シグナル注文後、`CooldownBars = 120` 本の確定足の間、次のシグナル判断を待ちます。約定済み建玉には実際の約定価格から `StopLossPercent = 2` のローカル標準保護が別途働き、bid/ask 監視で成行決済します。保護は待機期間に依存しません。

`SeasonalityHistoryFile` が必須です。月次始値・終値を含む UTF-8 CSV で、ヘッダーは `SecurityId,Month,Open,Close`、`Month` は `yyyy-MM`、小数点はピリオドです。価格は正、同一銘柄の月は重複不可です。他の証券 ID は無視します。未指定、不正な行、対象銘柄のデータがない場合は起動できません。

各シグナル足の `S` は、過去 `SeasonalityLookbackYears = 10` 年の同じ暦月における `100 × (Close / Open − 1)` の平均です。`MinimumSeasonalSamples = 2` 件以上が必要です。ファイルにあっても当月や将来月は使いません。履歴不足時は新規建玉を作りません。実際の銘柄の月次価格を入力し、固定の月別スコア表は使用しません。
