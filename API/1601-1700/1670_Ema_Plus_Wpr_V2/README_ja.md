# EMA プラス WPR v2 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Williams %Rオシレーターとトレンドフィルターに EMA を組み合わせた戦略。押し目や戻りの後にWPRが極値に達したときにトレードします。オプションのWPRベースのエグジット、トレーリングストップ、バー数ベースのエグジットを含みます。

## 詳細

- **ロング**: 押し目後にWPRが-100に達し、EMAトレンドが上昇中。
- **ショート**: 戻りの後にWPRが0に達し、EMAトレンドが下降中。
- **インジケーター**: Williams %R、EMA。
- **ストップ**: 固定ストップロスとテイクプロフィット、オプションのトレーリングストップ。
