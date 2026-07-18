# XRP AI 15分足適応型戦略 v3.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は上位時間軸のトレンドフィルターを使用して XRP を 15 分足で取引します。小さなプルバック、中程度のボリュームフラッシュ、大きなモメンタムバーストのいずれかを選択し、ATR ベースのストップ、ターゲット、トレーリングストップ、時間ベースのエグジットを適用します。

## パラメーター
- **Risk Mult** – 初期ストップの ATR 倍数。
- **Small TP** – 小さなプルバック時のテイクプロフィット ATR 倍数。
- **Med TP** – 中程度のボリュームフラッシュ時のテイクプロフィット ATR 倍数。
- **Large TP** – 大きなモメンタムバースト時のテイクプロフィット ATR 倍数。
- **Volume Mult** – スパイクを検出するための SMA-20 ボリューム倍数。
- **Trail Percent** – 最高値からの ATR のトレーリングストップ割合。
- **Trail Arm** – トレーリング発動前の ATR 倍数でのオープン利益。
- **Max Bars** – ポジションを保有する最大 15 分足本数。
- **Candle Type** – メイン計算に使用するローソク足の種類。
- **Trend Candle Type** – トレンドフィルターに使用するローソク足の種類。
