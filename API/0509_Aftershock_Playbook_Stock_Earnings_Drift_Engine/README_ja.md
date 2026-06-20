# Aftershock Playbook 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Aftershock Playbook** 戦略は、EPS サプライズに基づく決算後のドリフトを取引します。

- **エントリー**: 決算発表時、サプライズ ≥ `PositiveSurprise` でロング、サプライズ ≤ `NegativeSurprise` でショート。`ReverseSignals` でシグナルを反転できます。
- **ストップ**: オプションの ATR ストップ（`AtrLength`、`AtrMultiplier`）をショートポジションに適用。
- **エグジット**: `UseTimeExit` を有効にした場合、`HoldDays` 暦日後にポジションを閉じます。
- **再エントリー**: 利益を出して決済した後、戦略は同方向に一度だけ再エントリーします。損失取引は次回の決算発表まで新規エントリーをブロックします。

*外部の決算データフィードが必要です。*
