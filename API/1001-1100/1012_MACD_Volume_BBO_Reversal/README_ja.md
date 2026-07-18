# MACD 出来高 BBO リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

出来高オシレーターと MACD のゼロライン・クロス、シグナルライン比較を組み合わせた戦略。
MACD がゼロラインを上抜け、出来高オシレーターが正で MACD がシグナル線の上にあるときにロングエントリーします。
ショートエントリーは対称的です。ストップロスは直近の安値/高値を使用し、テイクプロフィットはリスク・リワード比に基づきます。

## パラメーター
- `VolumeShortLength` – 出来高の短期 EMA 期間（デフォルト: 6）
- `VolumeLongLength` – 出来高の長期 EMA 期間（デフォルト: 12）
- `MacdFastLength` – MACD の速い MA 期間（デフォルト: 11）
- `MacdSlowLength` – MACD の遅い MA 期間（デフォルト: 21）
- `MacdSignalLength` – MACD のシグナルライン期間（デフォルト: 10）
- `LookbackPeriod` – 直近の高値/安値を計算するバー数（デフォルト: 10）
- `RiskReward` – テイクプロフィット対ストップロス比（デフォルト: 1.5）
- `CandleType` – ローソク足の時間軸（デフォルト: 5 分）
