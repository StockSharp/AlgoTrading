# Keltner季節フィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

過去の月次リターンで絞り込むケルトナーチャネルのブレイク戦略です。確定5分足で `E = EMA(20)`、`A = ATR(14)`、`M = AtrMultiplier = 2`、`U = E + M × A`、`L = E − M × A` を用い、`SeasonalThreshold = 0.5` パーセントポイントです。指標形成と売買許可を待ちます。

`S > SeasonalThreshold`、`Close > U`、`Position <= 0` なら買い、`S < -SeasonalThreshold`、`Close < L`、`Position >= 0` なら売りです。数量 `Volume + abs(Position)` で反対建玉を全反転します。シグナル終値基準で、ロングは `Close − M × A`、ショートは `Close + M × A` にストップを記録します。

確定足で最初にストップを確認し、ロングは `Close <= stop`、ショートは `Close >= stop` なら決済し、ストップを消してその足の処理を終えます。その他はエントリーを確認した後、残ったロングは E 未満、ショートは E 超で決済します。これはシグナル終値を基準に確定足で判定する手動ストップです。足の途中の取引所ストップ、利確、トレーリングはありません。

`SeasonalityHistoryFile` が必須です。月次始値・終値を含む UTF-8 CSV で、ヘッダーは `SecurityId,Month,Open,Close`、`Month` は `yyyy-MM`、小数点はピリオドです。価格は正、同一銘柄の月は重複不可です。他の証券 ID は無視します。未指定、不正な行、対象銘柄のデータがない場合は起動できません。

各シグナル足の `S` は、過去 `SeasonalityLookbackYears = 10` 年の同じ暦月における `100 × (Close / Open − 1)` の平均です。`MinimumSeasonalSamples = 2` 件以上が必要です。ファイルにあっても当月や将来月は使いません。履歴不足時は新規建玉を作りません。実際の銘柄の月次価格を入力し、固定の月別スコア表は使用しません。
